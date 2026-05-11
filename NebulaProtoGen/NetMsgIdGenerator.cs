using System.Text;
using System.Text.RegularExpressions;

namespace NebulaProtoGen;

internal class NetMsgIdGenerator
{
	public static void GenerateNetMsgIdEnum(string outputPath, Dictionary<ulong, ArcxEntry> dataEntries, byte[] cryptKey)
	{
		ArcxEntry? arcxEntry = Program.GetDataFromPath("GameCore/Network/NetMsgId.lua");
		if (arcxEntry == null)
		{
			Program.logger.Error("NetMsgId.lua not found in data entries.");
			return;
		}
		byte[] decryptedData = XXTeaHelper.Decrypt(arcxEntry.data, Program.CRYPT_TEXT_ASSET_KEY);
		string luaSource = unluac.MainProgram.DecompileToString(decryptedData);
		//File.WriteAllText("NetMsgId.lua", luaSource);
		Dictionary<string, int> idMap = ParseNetMsgIdLua(luaSource);
		Dictionary<int, string> msgNameMap = ParseNetMsgNameLua(luaSource);
		idMap = idMap.OrderBy(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
		StringBuilder enumBuilder = new();
		enumBuilder.AppendLine("package emu.nebula.net;");
		enumBuilder.AppendLine();
		enumBuilder.AppendLine("public class NetMsgId {");
		enumBuilder.AppendLine("\tpublic static final int none = 0;");
		foreach (var kv in idMap)
		{
			enumBuilder.AppendLine($"\tpublic static final int {kv.Key} = {kv.Value};");
		}
		enumBuilder.AppendLine("}");
		File.WriteAllText(outputPath, enumBuilder.ToString());
		Program.logger.Info($"Generated NetMsgId enum with {idMap.Count} entries to {outputPath}");
		Dictionary<string, string> packetLookup = new Dictionary<string, string>();
		foreach (var kv in idMap)
		{
			if (msgNameMap.TryGetValue(kv.Value, out string? msgName))
			{
				packetLookup[kv.Key] = msgName;
			}
			else
			{
				packetLookup[kv.Key] = "???";
			}
		}
		Program.GeneratePacketLookupFile("NetMsgId", packetLookup);
	}

	public static Dictionary<string, int> ParseNetMsgIdLua(string lua)
	{
		Regex entryRegex = new Regex(
			@"(?<name>[A-Za-z0-9_]+)\s*=\s*(?<value>-?\d+)",
			RegexOptions.Compiled);

		var result = new Dictionary<string, int>();

		foreach (Match match in entryRegex.Matches(lua))
		{
			string name = match.Groups["name"].Value;
			int value = int.Parse(match.Groups["value"].Value);

			// Only store entries inside NetMsgId.Id
			if (!lua.Contains("NetMsgId.Id"))
				continue;

			// Ignore table definitions and other keys
			if (name == "NetMsgId" || name == "Id" || name == "MsgName")
				continue;

			if (!result.ContainsKey(name))
				result.Add(name, value);
		}

		return result;
	}

	public static Dictionary<int, string> ParseNetMsgNameLua(string lua)
	{
		var msgNameTable = ExtractMsgNameTable(lua);

		var result = new Dictionary<int, string>();

		Regex entryRegex = new Regex(
			@"\[(?<id>-?\d+)\]\s*=\s*""(?<name>[^""]+)""",
			RegexOptions.Compiled);

		foreach (Match match in entryRegex.Matches(msgNameTable))
		{
			int id = int.Parse(match.Groups["id"].Value);
			string name = match.Groups["name"].Value;

			if (!result.ContainsKey(id))
				result.Add(id, name);
		}

		return result;
	}

	private static string ExtractMsgNameTable(string lua)
	{
		var regex = new Regex(
			@"NetMsgId\.MsgName\s*=\s*\{(?<content>[\s\S]*?)\}",
			RegexOptions.Compiled);

		var match = regex.Match(lua);
		if (!match.Success)
			throw new Exception("Could not locate NetMsgId.MsgName table in Lua file.");

		return match.Groups["content"].Value;
	}
}
