using google.protobuf;
using Newtonsoft.Json;
using ProtoBuf;
using ProtoDescDumper.App;
using ProtoDescDumper.Core;
using System.Security.Cryptography;
using System.Text;

namespace NebulaProtoGen;

class Program
{
	public static readonly byte[] CRYPT_TEXT_ASSET_KEY = ComputeCryptKey();
	private const string protoPath = "Game/Adventure/StarTower/roguelike_tempData.pb";
	private const string protoPath2 = "GameCore/Network/proto.pb";
	private const string protoPath3 = "Game/CodeGen/table.pb";
	private static Dictionary<ulong, ArcxEntry> dataEntries = [];
	private const string baseOutputDir = "Generated";
	public static Logger logger = new();
	private const bool enableNetMsgIdGen = true;

	static void Main(string[] args)
	{
		logger.Info("Starting NebulaProtoGen...");

		string filePath = args.Length > 0 ? args[0] : "ClientConfig.json";

		VendorEnvData vendor;

		if (File.Exists(filePath))
		{
			logger.Info($"Reading ClientConfig from {filePath}...");
			ClientConfig clientConfig = JsonConvert.DeserializeObject<ClientConfig>(File.ReadAllText(filePath))!;
			vendor = clientConfig.vendorEnvData.First(v => v.name == "EN");
		}
		else
		{
			logger.Info($"ClientConfig file not found at {filePath}. Using built-in default configuration.");
			vendor = new VendorEnvData
			{
				name = "EN",
				vendorDisplayName = "EN",
				flags = Flags.ForeignChannel,
				clientVersion_Android = "0.3.0",
				clientVersion_IOS = "0.3.0",
				clientVersion_Windows = "0.3.0",
				timeZone = 8,
				localLanguage = "en_US",
				voiceLanguage = "ja_JP",
				availableTextLanguages = [
					"en_US"
				],
				availableVoiceLanguages = [
					"ja_JP",
					"zh_CN"
				],
				sdkName = "EN",
				serverURL = "https://nova-static.stellasora.global",
				serverChannelName = "Official",
				serverMetaKey = "ma5Dn2FhC*Xhxy%c",
				reviewServerMetaKey = "ma5Dn2FhC*Xhxy%c",
				serverGarbleKey = "xNdVF^XTa6T3HCUATMQ@sKMLzAw&%L!3",
				reviewServerGarbleKey = "xNdVF^XTa6T3HCUATMQ@sKMLzAw&%L!3"
			};
		}

		ManifestApp app = new();

		ClientDiff? ret = app.FetchVendor(vendor);
		if (ret == null)
		{
			logger.Error($"FetchVendor returned null for '{vendor.name}'.");
			return;
		}
		DownloadManager mgr = new DownloadManager(vendor, logger);
		FileDiff? luaArcx = GetDiffByName(ret, "lua.arcx");
		if (luaArcx == null)
		{
			logger.Error("Could not find lua.arcx in the client diff.");
			return;
		}
		(byte[] arcxBytes, long effectiveVersion) = mgr.DownloadAndProcess(luaArcx, ret);
		logger.Info($"{luaArcx.FileName} effective version: {effectiveVersion}");

		List<ArcxEntry> loadedArcx = ArcxExtract.ParseData(arcxBytes);
		dataEntries = loadedArcx.ToDictionary(e => e.hash);

		foreach (string strPath in new[] { protoPath, protoPath2, protoPath3 })
		{
			ArcxEntry? entry = GetDataFromPath(strPath);
			if (entry == null)
			{
				logger.Error($"Could not find entry for the path {strPath}.");
				continue;
			}
			logger.Info($"Extracting {strPath}...");
			string outDir = Path.Combine(baseOutputDir, Path.GetFileNameWithoutExtension(strPath) ?? "");
			using MemoryStream mspb = new(XXTeaHelper.Decrypt(entry.data, CRYPT_TEXT_ASSET_KEY));
			FileDescriptorSet descSet = Serializer.Deserialize<FileDescriptorSet>(mspb);
			// File.WriteAllBytes(Path.GetFileName(strPath), mspb.ToArray());
			var fileSystem = new LocalFileSystem();
			var coreService = new ProtoDescriptorService([], logger);
			var service = new ProtoDumpService(fileSystem, logger, coreService, coreService);
			service.Run(descSet, outDir);
		}

		if (enableNetMsgIdGen)
		{
			NetMsgIdGenerator.GenerateNetMsgIdEnum(
				Path.Combine(baseOutputDir, "NetMsgId.java"),
				dataEntries,
				CRYPT_TEXT_ASSET_KEY);
			logger.Info("NetMsgId enum generated.");
		}
	}

	private static FileDiff? GetDiffByName(ClientDiff data, string name)
		=> data.Diffs.FirstOrDefault(e => e.FileName == name);

	public static void GeneratePacketLookupFile(string enumName, Dictionary<string, string> packetLookup)
	{
		// we make it json
		string outputPath = Path.Combine(baseOutputDir, $"{enumName}.json");
		File.WriteAllText(outputPath, System.Text.Json.JsonSerializer.Serialize(packetLookup, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
		logger.Info($"Generated packet lookup file at {outputPath}");
	}

	public static ulong HashPathUtf8(string path)
	{
		ArgumentNullException.ThrowIfNull(path);
		path = path.ToLowerInvariant();
		var bytes = Encoding.UTF8.GetBytes(path);
		return XxHash64.Hash64(bytes, seed: 0UL);
	}

	public static ArcxEntry? GetDataFromPath(string path)
	{
		ulong hash = HashPathUtf8(path);
		dataEntries.TryGetValue(hash, out ArcxEntry? value);
		return value;
	}

	private static byte[] ComputeCryptKey()
	{
		const uint kx = 255;
		const uint ky = 255;
		uint product = kx * ky;
		byte[] productBytes = BitConverter.GetBytes(product);
		return MD5.HashData(productBytes);
	}
}