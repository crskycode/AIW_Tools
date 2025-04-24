using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace ScriptTool
{
    internal class Script
    {
        private readonly static byte[] Signature = Encoding.ASCII.GetBytes("ADV_98 \0");

        private string _disassembly = string.Empty;

        public void Load(string filePath, Encoding encoding)
        {
            var reader = new BinaryReader(File.OpenRead(filePath));

            var signature = reader.ReadBytes(8);

            if (!signature.SequenceEqual(Signature))
            {
                throw new Exception("This is not a valid AIW script file.");
            }

            Parse(reader, encoding);

            reader.Close();
        }

        private static Dictionary<int, string> ParseStringPool(byte[] input, Encoding encoding)
        {
            var reader = new BinaryReader(new MemoryStream(input));

            var result = new Dictionary<int, string>();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var position = Convert.ToInt32(reader.BaseStream.Position);
                var value = reader.ReadNullTerminatedString(encoding);

                result.Add(position, value);
            }

            return result;
        }

        private void Parse(BinaryReader reader, Encoding encoding)
        {
            reader.BaseStream.Position = 8;

            reader.ReadInt32(); // unknow
            reader.ReadInt32(); // unknow

            var codePos = reader.ReadInt32();
            var stringPoolPos = reader.ReadInt32();

            reader.BaseStream.Position = stringPoolPos;
            var stringPoolLength = Convert.ToInt32(reader.BaseStream.Length) - stringPoolPos;
            var stringPoolData = reader.ReadBytes(stringPoolLength);
            var stringPool = ParseStringPool(stringPoolData, encoding);

            var stringRefs = new HashSet<int>();

            var dis = new StringBuilder();

            reader.BaseStream.Position = codePos;
            var codeEnd = stringPoolPos;

            while (reader.BaseStream.Position < codeEnd)
            {
                var addr = Convert.ToInt32(reader.BaseStream.Position);
                var code = reader.ReadByte();

                switch (code)
                {
                    case 0x00:
                    {
                        // id: 0x00
                        // handler: 0x40D760
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 1

                        dis.AppendLine($"{addr:X8} | nop");
                        break;
                    }
                    case 0x01:
                    {
                        // id: 0x01
                        // handler: 0x40D770
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        var target = reader.ReadInt32();
                        dis.AppendLine($"{addr:X8} | jump_false {target:X8}");
                        break;
                    }
                    case 0x02:
                    {
                        // id: 0x02
                        // handler: 0x40D7C0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        var target = addr + reader.ReadSByte();
                        dis.AppendLine($"{addr:X8} | jump_false_rel_8 {target:X8}");
                        break;
                    }
                    case 0x03:
                    {
                        // id: 0x03
                        // handler: 0x40D820
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        var target = reader.ReadInt32();
                        dis.AppendLine($"{addr:X8} | jump {target:X8}");
                        break;
                    }
                    case 0x04:
                    {
                        // id: 0x04
                        // handler: 0x40D870
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        var target = addr + reader.ReadSByte();
                        dis.AppendLine($"{addr:X8} | jump_rel_8 {target:X8}");
                        break;
                    }
                    case 0x05:
                    {
                        // id: 0x05
                        // handler: 0x40D8C0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        var target = reader.ReadInt32();
                        dis.AppendLine($"{addr:X8} | call {target:X8}");
                        break;
                    }
                    case 0x06:
                    {
                        // id: 0x06
                        // handler: 0x40D940
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        var target = addr + reader.ReadSByte();
                        dis.AppendLine($"{addr:X8} | call_rel_8 {target:X8}");
                        break;
                    }
                    case 0x07:
                    {
                        // id: 0x07
                        // handler: 0x40D9B0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | ret");
                        break;
                    }
                    case 0x08:
                    {
                        // id: 0x08
                        // handler: 0x40DA00
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | pop_call");
                        break;
                    }
                    case 0x09:
                    {
                        // id: 0x09
                        // handler: 0x40DA40
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        reader.ReadInt32();
                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x0A:
                    {
                        // id: 0x0A
                        // handler: 0x40DB90
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        reader.ReadInt32(); // 0xFFFFFFFF
                        dis.AppendLine($"{addr:X8} | call_script");
                        break;
                    }
                    case 0x0B:
                    {
                        // id: 0x0B
                        // handler: 0x40DBD0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | ret_script");
                        break;
                    }
                    case 0x0C:
                    {
                        // id: 0x0C
                        // handler: 0x40DC30
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | exit_script");
                        break;
                    }
                    case 0x0D:
                    {
                        // id: 0x0D
                        // handler: 0x40DC40
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        int value = reader.ReadInt32();

                        // Maybe it's the string reference

                        if (value <= -65 || value >= -32)
                        {
                            if (unchecked((uint)value) < 0xFFFFFFE0)
                            {
                                if (stringPool.TryGetValue(value, out var s))
                                {
                                    stringRefs.Add(value);
                                    s = s.Escape();
                                    dis.AppendLine($"{addr:X8} | push dword 0x{value:X8} ; offset \"{s}\"");
                                    break;
                                }
                            }
                        }

                        dis.AppendLine($"{addr:X8} | push dword 0x{value:X8}");
                        break;
                    }
                    case 0x0E:
                    {
                        // id: 0x0E
                        // handler: 0x40DC90
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        int value = reader.ReadByte();
                        // Maybe it's the string reference

                        if (value <= -65 || value >= -32)
                        {
                            if (unchecked((uint)value) < 0xFFFFFFE0)
                            {
                                if (stringPool.TryGetValue(value, out var s))
                                {
                                    stringRefs.Add(value);
                                    s = s.Escape();
                                    dis.AppendLine($"{addr:X8} | push dword 0x{value:X8} ; offset \"{s}\"");
                                    break;
                                }
                            }
                        }

                        dis.AppendLine($"{addr:X8} | push byte 0x{value:X2}");
                        break;
                    }
                    case 0x0F:
                    {
                        // id: 0x0F
                        // handler: 0x0
                        // handler2: 0x40DCE0
                        // flags: 0
                        // time_cost: 2

                        reader.ReadByte();
                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x10:
                    {
                        // id: 0x10
                        // handler: 0x0
                        // handler2: 0x40DD40
                        // flags: 0
                        // time_cost: 2

                        reader.ReadByte();
                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x11:
                    {
                        // id: 0x11
                        // handler: 0x0
                        // handler2: 0x40DDA0
                        // flags: 0
                        // time_cost: 2

                        reader.ReadByte();
                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x12:
                    {
                        // id: 0x12
                        // handler: 0x410430
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | pop");
                        break;
                    }
                    case 0x13:
                    {
                        // id: 0x13
                        // handler: 0x0
                        // handler2: 0x40DE00
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x14:
                    {
                        // id: 0x14
                        // handler: 0x0
                        // handler2: 0x40DE20
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x15:
                    {
                        // id: 0x15
                        // handler: 0x0
                        // handler2: 0x40DE40
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x16:
                    {
                        // id: 0x16
                        // handler: 0x0
                        // handler2: 0x40DE60
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x17:
                    {
                        // id: 0x17
                        // handler: 0x0
                        // handler2: 0x40DE90
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x18:
                    {
                        // id: 0x18
                        // handler: 0x0
                        // handler2: 0x40DEC0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x19:
                    {
                        // id: 0x19
                        // handler: 0x0
                        // handler2: 0x40DEF0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x1A:
                    {
                        // id: 0x1A
                        // handler: 0x0
                        // handler2: 0x40DF20
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x1B:
                    {
                        // id: 0x1B
                        // handler: 0x0
                        // handler2: 0x40DF50
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x1C:
                    {
                        // id: 0x1C
                        // handler: 0x0
                        // handler2: 0x40DF80
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x1D:
                    {
                        // id: 0x1D
                        // handler: 0x0
                        // handler2: 0x40DFB0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x1E:
                    {
                        // id: 0x1E
                        // handler: 0x0
                        // handler2: 0x40DFE0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x1F:
                    {
                        // id: 0x1F
                        // handler: 0x0
                        // handler2: 0x40E010
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x20:
                    {
                        // id: 0x20
                        // handler: 0x0
                        // handler2: 0x40E030
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x21:
                    {
                        // id: 0x21
                        // handler: 0x0
                        // handler2: 0x40E050
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x22:
                    {
                        // id: 0x22
                        // handler: 0x40E070
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | neg");
                        break;
                    }
                    case 0x23:
                    {
                        // id: 0x23
                        // handler: 0x40E080
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | add");
                        break;
                    }
                    case 0x24:
                    {
                        // id: 0x24
                        // handler: 0x40E0A0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | sub");
                        break;
                    }
                    case 0x25:
                    {
                        // id: 0x25
                        // handler: 0x40E0C0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | mul");
                        break;
                    }
                    case 0x26:
                    {
                        // id: 0x26
                        // handler: 0x40E0E0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | div");
                        break;
                    }
                    case 0x27:
                    {
                        // id: 0x27
                        // handler: 0x40E100
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | mod");
                        break;
                    }
                    case 0x28:
                    {
                        // id: 0x28
                        // handler: 0x40E120
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | or");
                        break;
                    }
                    case 0x29:
                    {
                        // id: 0x29
                        // handler: 0x40E140
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | and");
                        break;
                    }
                    case 0x2A:
                    {
                        // id: 0x2A
                        // handler: 0x40E160
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | xor");
                        break;
                    }
                    case 0x2B:
                    {
                        // id: 0x2B
                        // handler: 0x40E180
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | not");
                        break;
                    }
                    case 0x2C:
                    {
                        // id: 0x2C
                        // handler: 0x40E190
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | logical_or");
                        break;
                    }
                    case 0x2D:
                    {
                        // id: 0x2D
                        // handler: 0x40E1C0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | logical_and");
                        break;
                    }
                    case 0x2E:
                    {
                        // id: 0x2E
                        // handler: 0x40E200
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | is_zero");
                        break;
                    }
                    case 0x2F:
                    {
                        // id: 0x2F
                        // handler: 0x40E220
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | equal");
                        break;
                    }
                    case 0x30:
                    {
                        // id: 0x30
                        // handler: 0x40E240
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | not_equal");
                        break;
                    }
                    case 0x31:
                    {
                        // id: 0x31
                        // handler: 0x40E260
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | greater");
                        break;
                    }
                    case 0x32:
                    {
                        // id: 0x32
                        // handler: 0x40E280
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | less");
                        break;
                    }
                    case 0x33:
                    {
                        // id: 0x33
                        // handler: 0x40E2A0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | greater_equal");
                        break;
                    }
                    case 0x34:
                    {
                        // id: 0x34
                        // handler: 0x40E2C0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | less_equal");
                        break;
                    }
                    case 0x35:
                    {
                        // id: 0x35
                        // handler: 0x0
                        // handler2: 0x40E2E0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x36:
                    {
                        // id: 0x36
                        // handler: 0x0
                        // handler2: 0x40E300
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x37:
                    {
                        // id: 0x37
                        // handler: 0x40E330
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | clear_string");
                        break;
                    }
                    case 0x38:
                    {
                        // id: 0x38
                        // handler: 0x40E350
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | strcpy");
                        break;
                    }
                    case 0x39:
                    {
                        // id: 0x39
                        // handler: 0x40E380
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | strcat");
                        break;
                    }
                    case 0x3A:
                    {
                        // id: 0x3A
                        // handler: 0x40E3B0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | strcmp");
                        break;
                    }
                    case 0x3B:
                    {
                        // id: 0x3B
                        // handler: 0x40E3E0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x3C:
                    {
                        // id: 0x3C
                        // handler: 0x40E4D0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | update_tick");
                        break;
                    }
                    case 0x3D:
                    {
                        // id: 0x3D
                        // handler: 0x40E4F0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x3E:
                    {
                        // id: 0x3E
                        // handler: 0x40E560
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x3F:
                    {
                        // id: 0x3F
                        // handler: 0x40E670
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x40:
                    {
                        // id: 0x40
                        // handler: 0x40E7E0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x41:
                    {
                        // id: 0x41
                        // handler: 0x40E990
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x42:
                    {
                        // id: 0x42
                        // handler: 0x40E910
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x43:
                    {
                        // id: 0x43
                        // handler: 0x40E930
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x44:
                    {
                        // id: 0x44
                        // handler: 0x40E950
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x45:
                    {
                        // id: 0x45
                        // handler: 0x40EA70
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x46:
                    {
                        // id: 0x46
                        // handler: 0x40EAB0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x47:
                    {
                        // id: 0x47
                        // handler: 0x40EBF0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x48:
                    {
                        // id: 0x48
                        // handler: 0x40EB10
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x49:
                    {
                        // id: 0x49
                        // handler: 0x40EC40
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x4A:
                    {
                        // id: 0x4A
                        // handler: 0x40EC50
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x4B:
                    {
                        // id: 0x4B
                        // handler: 0x40EC70
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x4C:
                    {
                        // id: 0x4C
                        // handler: 0x40EDA0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x4D:
                    {
                        // id: 0x4D
                        // handler: 0x40EDC0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x4E:
                    {
                        // id: 0x4E
                        // handler: 0x40EE00
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x4F:
                    {
                        // id: 0x4F
                        // handler: 0x40F180
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x50:
                    {
                        // id: 0x50
                        // handler: 0x40F190
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x51:
                    {
                        // id: 0x51
                        // handler: 0x40F1D0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x52:
                    {
                        // id: 0x52
                        // handler: 0x40F210
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x53:
                    {
                        // id: 0x53
                        // handler: 0x40F220
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x54:
                    {
                        // id: 0x54
                        // handler: 0x40F250
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x55:
                    {
                        // id: 0x55
                        // handler: 0x40F250
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x56:
                    {
                        // id: 0x56
                        // handler: 0x40FC90
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x57:
                    {
                        // id: 0x57
                        // handler: 0x40F260
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x58:
                    {
                        // id: 0x58
                        // handler: 0x40F2A0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x59:
                    {
                        // id: 0x59
                        // handler: 0x40F300
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x5A:
                    {
                        // id: 0x5A
                        // handler: 0x40F380
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x5B:
                    {
                        // id: 0x5B
                        // handler: 0x40F3F0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x5C:
                    {
                        // id: 0x5C
                        // handler: 0x40F490
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x5D:
                    {
                        // id: 0x5D
                        // handler: 0x40F720
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x5E:
                    {
                        // id: 0x5E
                        // handler: 0x40F7B0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x5F:
                    {
                        // id: 0x5F
                        // handler: 0x40F840
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x60:
                    {
                        // id: 0x60
                        // handler: 0x40F8B0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x61:
                    {
                        // id: 0x61
                        // handler: 0x40F8C0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x62:
                    {
                        // id: 0x62
                        // handler: 0x40F8F0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x63:
                    {
                        // id: 0x63
                        // handler: 0x40F900
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 16

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x64:
                    {
                        // id: 0x64
                        // handler: 0x40F990
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 16

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x65:
                    {
                        // id: 0x65
                        // handler: 0x40FA40
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x66:
                    {
                        // id: 0x66
                        // handler: 0x40FAE0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 16

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x67:
                    {
                        // id: 0x67
                        // handler: 0x40FBE0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 16

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x68:
                    {
                        // id: 0x68
                        // handler: 0x40FC40
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 16

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x69:
                    {
                        // id: 0x69
                        // handler: 0x40FE50
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x6A:
                    {
                        // id: 0x6A
                        // handler: 0x40FE70
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x6B:
                    {
                        // id: 0x6B
                        // handler: 0x40FE90
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x6C:
                    {
                        // id: 0x6C
                        // handler: 0x40FEA0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x6D:
                    {
                        // id: 0x6D
                        // handler: 0x40FEE0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x6E:
                    {
                        // id: 0x6E
                        // handler: 0x40FF40
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x6F:
                    {
                        // id: 0x6F
                        // handler: 0x40FF50
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x70:
                    {
                        // id: 0x70
                        // handler: 0x40FF80
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x71:
                    {
                        // id: 0x71
                        // handler: 0x40FFA0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x72:
                    {
                        // id: 0x72
                        // handler: 0x410070
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x73:
                    {
                        // id: 0x73
                        // handler: 0x410080
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x74:
                    {
                        // id: 0x74
                        // handler: 0x4100D0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x75:
                    {
                        // id: 0x75
                        // handler: 0x4100E0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x76:
                    {
                        // id: 0x76
                        // handler: 0x410140
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x77:
                    {
                        // id: 0x77
                        // handler: 0x410430
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        dis.AppendLine($"{addr:X8} | pop");
                        break;
                    }
                    case 0x78:
                    {
                        // id: 0x78
                        // handler: 0x410150
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x79:
                    {
                        // id: 0x79
                        // handler: 0x410200
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x7A:
                    {
                        // id: 0x7A
                        // handler: 0x410430
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 8

                        dis.AppendLine($"{addr:X8} | pop");
                        break;
                    }
                    case 0x7B:
                    {
                        // id: 0x7B
                        // handler: 0x410250
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        reader.ReadInt32();
                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x7C:
                    {
                        // id: 0x7C
                        // handler: 0x410300
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x7D:
                    {
                        // id: 0x7D
                        // handler: 0x410380
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x7E:
                    {
                        // id: 0x7E
                        // handler: 0x0
                        // handler2: 0x4103A0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x7F:
                    {
                        // id: 0x7F
                        // handler: 0x0
                        // handler2: 0x4103C0
                        // flags: 0
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x80:
                    {
                        // id: 0x80
                        // handler: 0x4103E0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 1

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x81:
                    {
                        // id: 0x81
                        // handler: 0x4103F0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 32

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x82:
                    {
                        // id: 0x82
                        // handler: 0x410410
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 32

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x83:
                    {
                        // id: 0x83
                        // handler: 0x410430
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | pop");
                        break;
                    }
                    case 0x84:
                    {
                        // id: 0x84
                        // handler: 0x410440
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x85:
                    {
                        // id: 0x85
                        // handler: 0x410450
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x86:
                    {
                        // id: 0x86
                        // handler: 0x4104A0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x87:
                    {
                        // id: 0x87
                        // handler: 0x4104E0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x88:
                    {
                        // id: 0x88
                        // handler: 0x410490
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x89:
                    {
                        // id: 0x89
                        // handler: 0x410610
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x8A:
                    {
                        // id: 0x8A
                        // handler: 0x4105E0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x8B:
                    {
                        // id: 0x8B
                        // handler: 0x0
                        // handler2: 0x40E050
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x8C:
                    {
                        // id: 0x8C
                        // handler: 0x0
                        // handler2: 0x40E030
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x8D:
                    {
                        // id: 0x8D
                        // handler: 0x0
                        // handler2: 0x40E010
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x8E:
                    {
                        // id: 0x8E
                        // handler: 0x0
                        // handler2: 0x40E300
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x8F:
                    {
                        // id: 0x8F
                        // handler: 0x0
                        // handler2: 0x40E2E0
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x90:
                    {
                        // id: 0x90
                        // handler: 0x0
                        // handler2: 0x40DFE0
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x91:
                    {
                        // id: 0x91
                        // handler: 0x0
                        // handler2: 0x40DFB0
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x92:
                    {
                        // id: 0x92
                        // handler: 0x0
                        // handler2: 0x40DF80
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x93:
                    {
                        // id: 0x93
                        // handler: 0x0
                        // handler2: 0x40DDA0
                        // flags: 1
                        // time_cost: 2

                        reader.ReadByte();
                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x94:
                    {
                        // id: 0x94
                        // handler: 0x0
                        // handler2: 0x40DD40
                        // flags: 1
                        // time_cost: 2

                        reader.ReadByte();
                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x95:
                    {
                        // id: 0x95
                        // handler: 0x0
                        // handler2: 0x40DCE0
                        // flags: 1
                        // time_cost: 2

                        reader.ReadByte();
                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x96:
                    {
                        // id: 0x96
                        // handler: 0x0
                        // handler2: 0x40DE40
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x97:
                    {
                        // id: 0x97
                        // handler: 0x0
                        // handler2: 0x40DE20
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x98:
                    {
                        // id: 0x98
                        // handler: 0x0
                        // handler2: 0x40DE00
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x99:
                    {
                        // id: 0x99
                        // handler: 0x0
                        // handler2: 0x40DEC0
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x9A:
                    {
                        // id: 0x9A
                        // handler: 0x0
                        // handler2: 0x40DE90
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x9B:
                    {
                        // id: 0x9B
                        // handler: 0x0
                        // handler2: 0x40DE60
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x9C:
                    {
                        // id: 0x9C
                        // handler: 0x0
                        // handler2: 0x40DF50
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x9D:
                    {
                        // id: 0x9D
                        // handler: 0x0
                        // handler2: 0x40DF20
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x9E:
                    {
                        // id: 0x9E
                        // handler: 0x0
                        // handler2: 0x40DEF0
                        // flags: 1
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0x9F:
                    {
                        // id: 0x9F
                        // handler: 0x0
                        // handler2: 0x4103A0
                        // flags: 1
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xA0:
                    {
                        // id: 0xA0
                        // handler: 0x0
                        // handler2: 0x4103C0
                        // flags: 1
                        // time_cost: 2

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xA1:
                    {
                        // id: 0xA1
                        // handler: 0x410C50
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xA2:
                    {
                        // id: 0xA2
                        // handler: 0x410810
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xA3:
                    {
                        // id: 0xA3
                        // handler: 0x410C70
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xA4:
                    {
                        // id: 0xA4
                        // handler: 0x410D60
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xA5:
                    {
                        // id: 0xA5
                        // handler: 0x40F020
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xA6:
                    {
                        // id: 0xA6
                        // handler: 0x411420
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xA7:
                    {
                        // id: 0xA7
                        // handler: 0x411520
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xA8:
                    {
                        // id: 0xA8
                        // handler: 0x4115E0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xA9:
                    {
                        // id: 0xA9
                        // handler: 0x40EEF0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 16

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xAA:
                    {
                        // id: 0xAA
                        // handler: 0x411660
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 4

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xAB:
                    {
                        // id: 0xAB
                        // handler: 0x4116F0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xAC:
                    {
                        // id: 0xAC
                        // handler: 0x411A60
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xAD:
                    {
                        // id: 0xAD
                        // handler: 0x411780
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xAE:
                    {
                        // id: 0xAE
                        // handler: 0x411A90
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xAF:
                    {
                        // id: 0xAF
                        // handler: 0x411AD0
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xB0:
                    {
                        // id: 0xB0
                        // handler: 0x40F250
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xB1:
                    {
                        // id: 0xB1
                        // handler: 0x40F250
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    case 0xB2:
                    {
                        // id: 0xB2
                        // handler: 0x40EB50
                        // handler2: 0x0
                        // flags: 0
                        // time_cost: 64

                        dis.AppendLine($"{addr:X8} | opcode_{code:X1}");
                        break;
                    }
                    default:
                    {
                        throw new Exception($"Unknow opcode {code:X2} at {addr:X8} .");
                    }
                }
            }

            dis.AppendLine($"{stringPoolPos:X8} | ; Strings");

            var stringList = stringPool.OrderBy(x => x.Key)
                .ToList();

            foreach (var item in stringList)
            {
                var addr = stringPoolPos + item.Key;
                var s = item.Value.Escape();
                dis.AppendLine($"{addr:X8} | string \"{s}\"");
            }

            _disassembly = dis.ToString();
        }

        public void ExportDisasm(string filePath)
        {
            File.WriteAllText(filePath, _disassembly);
        }
    }
}
