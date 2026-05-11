using unluac.decompile;
using unluac.parse;
using unluac.util;
using static unluac.decompile.Output;

namespace unluac;

public static class MainProgram
{
	public static string version = "1.2.2.155";

	/*
    public static void Main(string[] args) {
        string? fn = null;
        var config = new Configuration();
        foreach(var arg in args) {
            if(arg.StartsWith("-")) {
                if(arg == "--rawstring") {
                    config.rawstring = true;
                } else {
                    Error("unrecognized option: " + arg, true);
                }
            } else if(fn == null) {
                fn = arg;
            } else {
                Error("too many arguments: " + arg, true);
            }
        }
        if(fn == null) {
            Error("no input file provided", true);
        } else {
            try {
                var lmain = FileToFunction(fn, config);
                var func = new Function(lmain);
                // Run full decompilation and print reconstructed Lua source
                var decompiler = new unluac.decompile.Decompiler(lmain);
                decompiler.Decompile();
                decompiler.Print();
                // Optional diagnostic footer
                Console.WriteLine("--[[ unluac C# version " + version + " | Lua version 0x" + lmain.header.version.versionNumber.ToString("X") + " ]]\n");
                Environment.Exit(0);
            } catch(IOException e) {
                Error(e.Message, false);
            }
        }
    }
    */

	public static string DecompileToString(byte[] input)
	{
		var buffer = new ByteBuffer(input);
		var header = new BHeader(buffer, new Configuration());
		var func = new Function(header.main);
		var decompiler = new unluac.decompile.Decompiler(header.main);
		decompiler.Decompile();
		using var sw = new StringWriter();
		Output output = new Output(new StringOutputProvider(sw));
		decompiler.Print(output);
		//sw.WriteLine("--[[ unluac C# version " + version + " | Lua version 0x" + lmain.header.version.versionNumber.ToString("X") + " ]]");
		return sw.ToString();
	}

	private static void Error(string err, bool usage)
	{
		Console.Error.WriteLine("unluac v" + version);
		Console.Error.Write("  error: ");
		Console.Error.WriteLine(err);
		if (usage)
		{
			Console.Error.WriteLine("  usage: dotnet run --project src_csharp/unluac/unluac.csproj [options] <file>");
		}
		Environment.Exit(1);
	}

	private static LFunction FileToFunction(string fn, Configuration config)
	{
		byte[] bytes = File.ReadAllBytes(fn);
		var buffer = new ByteBuffer(bytes);
		var header = new BHeader(buffer, config);
		return header.main;
	}

	public static void Decompile(string input, string output)
	{
		var lmain = FileToFunction(input, new Configuration());
		using var pout = new StreamWriter(output);
		pout.WriteLine("-- Decompiler not yet implemented in C#");
		pout.WriteLine("-- Lua version 0x" + lmain.header.version.versionNumber.ToString("X"));
		pout.Flush();
	}
}
