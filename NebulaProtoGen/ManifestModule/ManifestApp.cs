using NebulaProtoGen.Proto;
using ProtoBuf;
using System.Security.Cryptography;
using System.Text;

namespace NebulaProtoGen;

public class ManifestApp
{
	public ClientDiff? _clientDiff;
	public Logger _logger;

	public ManifestApp()
	{
		_logger = new Logger();
	}

	public ClientDiff? FetchVendor(VendorEnvData vendorEnvDatum)
	{
		string serverURL = vendorEnvDatum.serverURL;
		string serverMetaKey = vendorEnvDatum.serverMetaKey;

		if (serverURL.StartsWith("http://")) // ipv4
			return null;

		_logger = new Logger(this.GetType().Namespace ?? vendorEnvDatum.name);

		_logger.Info($"Getting meta HTML...");

		string metaUrl = serverURL + "/meta/win.html";

		using HttpClient httpClient = new HttpClient();
		byte[] htmlBytes = [];
		try
		{
			htmlBytes = httpClient.GetByteArrayAsync(metaUrl).Result;
		}
		catch (Exception)
		{
			_logger.Error($"{vendorEnvDatum.name} has no win.html");
			return null;
		}

		//_logger.Success($"Fetched meta HTML, size is {htmlBytes.Length} bytes.");
		_logger.Info($"Decrypting HTML...");

		//File.WriteAllBytes("win.html", htmlBytes);

		using MemoryStream output = new MemoryStream();
		try
		{
			byte[] iv = htmlBytes.Take(16).ToArray();
			byte[] encrypted = htmlBytes.Skip(16).ToArray();
			byte[] key = Encoding.UTF8.GetBytes(serverMetaKey);
			byte[] aesKey = new byte[16];
			Array.Copy(key, aesKey, Math.Min(key.Length, aesKey.Length));

			using Aes aes = Aes.Create();
			aes.Key = aesKey;
			aes.IV = iv;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;

			using MemoryStream ms = new MemoryStream(encrypted);
			using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
			cs.CopyTo(output);
		}
		catch (Exception)
		{
			_logger.Error($"Failed to decrypt {vendorEnvDatum.name} with given keys");
			return null;
		}

		//_logger.Success("Decrypted HTML successfully.");

		_clientDiff = Serializer.Deserialize<ClientDiff>(new MemoryStream(output.ToArray()));

		//_logger.Success($"Done.");

		byte[] serverresp = [];

		try
		{
			serverresp = httpClient.GetByteArrayAsync($"{vendorEnvDatum.serverURL}/meta/serverlist.html").Result;

			using MemoryStream output1 = new MemoryStream();
			byte[] iv = serverresp.Take(16).ToArray();
			byte[] encrypted = serverresp.Skip(16).ToArray();
			byte[] key = Encoding.UTF8.GetBytes(serverMetaKey);
			byte[] aesKey = new byte[16];
			Array.Copy(key, aesKey, Math.Min(key.Length, aesKey.Length));

			using Aes aes = Aes.Create();
			aes.Key = aesKey;
			aes.IV = iv;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;

			using MemoryStream ms = new MemoryStream(encrypted);
			using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
			cs.CopyTo(output1);

			ServerListMeta serverListMeta = Serializer.Deserialize<ServerListMeta>(new MemoryStream(output1.ToArray()))!;

			_logger.Info($"Fetched serverlist.html successfully. Server version: {serverListMeta.Version}");

		}
		catch (Exception ex)
		{
			_logger.Error($"Failed to fetch serverlist.html: {ex.Message}");
		}

		return _clientDiff;
	}

	public int GetVersionForFileName(string _fileName)
	{
		if (_clientDiff == null)
			return -1;
		FileDiff? entry = _clientDiff.Diffs.FirstOrDefault(e => e.FileName == _fileName);
		if (entry == null)
			throw new Exception($"No versions for {_fileName}");
		return Convert.ToInt32(entry.Version);
	}
}

public partial class ClientConfig
{
	public string buildVersion = String.Empty; // 0x18
	public string buildTag = String.Empty; // 0x20
	public bool isOpenGM; // 0x28
	public bool useLocalResourcesDownloadServer; // 0x29
	public string localResourcesDownloadServerUrl = String.Empty; // 0x30
	public string backupServerUrlPrefix = String.Empty; // 0x38
	public const string ProjectCode = "ss"; // Metadata: 0x01197695
	public const string ServerInfoURL = "/meta/serverlist.html"; // Metadata: 0x01197698
	public const string ServerDownloadURLFormat = "{ver}/{name}"; // Metadata: 0x011976AE
	public const string ServerResourceManifestURL = "/meta/win.html"; // Metadata: 0x011976BB
	public const string ServerDownloadURL = "/res/win/"; // Metadata: 0x011976CA
	public VendorEnvData[] vendorEnvData = []; // 0x40
	[NonSerialized]
	private VendorEnvData _currentVendorEnvData = new VendorEnvData(); // 0x48
	public static readonly string[] UsingAssetOverrideTags = []; // 0x18
	public const string Channel = "CN"; // Metadata: 0x011976D4
}

public partial class VendorEnvData
{
	public string name = String.Empty; // 0x10
	public string vendorDisplayName = String.Empty; // 0x18
	public Flags flags; // 0x20
	public string clientVersion_Android = String.Empty; // 0x28
	public string clientVersion_IOS = String.Empty; // 0x30
	public string clientVersion_Windows = String.Empty; // 0x38
	public int timeZone; // 0x40
	public string localLanguage = String.Empty; // 0x48
	public string voiceLanguage = String.Empty; // 0x50
	public string[] availableTextLanguages = []; // 0x58
	public string[] availableVoiceLanguages = []; // 0x60
	public string sdkName = String.Empty; // 0x68
	public string serverURL = String.Empty; // 0x70
	public string serverChannelName = String.Empty; // 0x78
	public string serverMetaKey = String.Empty; // 0x80
	public string reviewServerMetaKey = String.Empty; // 0x88
	public string serverGarbleKey = String.Empty; // 0x90
	public string reviewServerGarbleKey = String.Empty; // 0x98
}

[Flags]
public enum Flags // TypeDefIndex: 4402
{
	ForeignChannel = 1
}