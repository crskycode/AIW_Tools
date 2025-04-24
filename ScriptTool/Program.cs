using System.Text;

namespace ScriptTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("AIW Script Tool");
                Console.WriteLine("  created by Crsky");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  Disassemble : ScriptTool -d -in [input.bin] -icp [shift_jis] -out [output.txt]");
                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");

                Environment.ExitCode = 1;
                Console.ReadKey();

                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var parsedArgs = CommandLineParser.ParseArguments(args);

            // Common arguments
            CommandLineParser.EnsureArguments(parsedArgs, "-in", "-icp", "-out");

            var inputPath = Path.GetFullPath(parsedArgs["-in"]);
            var outputPath = Path.GetFullPath(parsedArgs["-out"]);
            var inputEncoding = Encoding.GetEncoding(parsedArgs["-icp"]);

            // Disassemble
            if (parsedArgs.ContainsKey("-d"))
            {
                var script = new Script();
                script.Load(inputPath, inputEncoding);
                script.ExportDisasm(outputPath);
                return;
            }
        }
    }
}
