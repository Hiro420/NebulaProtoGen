using System.Security.Cryptography;
using System.Text;

namespace NebulaProtoGen;

public static class DownloadStore
{
    public enum StorageRegion
    {
        Persistent = 0,
        Temporary = 1,
        Patch = 2
    }

    public enum DownloadResult
    {
        Success = 0,
        Fail = 1,
        WebRequestError = 2,
        ContentIsEmpty = 3,
        DataCheckFail = 4,
        DiskIsFull = 5,
        BinDiffPatchError = 6
    }

    public enum UpdateResult
    {
        Success = 0,
        Fail = 1,
        NeedUpdate = 2,
        NoUpdate = 3
    }

    public enum InstallResult
    {
        Success = 0,
        Fail = 1
    }

    public class DataCheckFail : Exception
    {
        public DataCheckFail() { }
    }

    public static string baseDataPath
    {
        get
        {
            if (_baseDataPath == null)
                _baseDataPath = Directory.GetParent(Environment.ProcessPath!)!.FullName;
            return _baseDataPath;
        }
    }

    public static string baseTemporaryCachePath
    {
        get
        {
            if (_baseTemporaryStorePath == null)
                _baseTemporaryStorePath = Path.Combine(baseDataPath, TemporaryStoreDirectoryName);
            return _baseTemporaryStorePath;
        }
    }

    public static string basePersistentCachePath
    {
        get
        {
            if (_basePersistentStorePath == null)
                _basePersistentStorePath = Path.Combine(baseDataPath, PersistentStoreDirectoryName);
            return _basePersistentStorePath;
        }
    }

    public static string basePatchCachePath
    {
        get
        {
            if (_basePatchCachePath == null)
                _basePatchCachePath = Path.Combine(baseDataPath, PatchStoreDirectoryName);
            return _basePatchCachePath;
        }
    }

    public static Action<string, string?>? onFileStore { get; set; }
    public static Action<string>? onFileDelete { get; set; }
    public static Action? onDownloadFinish { get; set; }
    public static HashSet<string> ActivedResourceManifestTags { get; private set; } = new HashSet<string>();

    public static void InitStore()
    {
        CreateStoreDirectory(StorageRegion.Temporary, null);
        CreateStoreDirectory(StorageRegion.Persistent, null);
        CreateStoreDirectory(StorageRegion.Patch, null);
        InitVersionMetaData();
    }

    public static void InitVersionMetaData()
    {
        string metaFilePath = Path.Combine(baseDataPath, VersionMetaDataFileName);
        if (File.Exists(metaFilePath))
        {
            ReadVersionMetaData(metaFilePath);
        }
        else
        {
            lock (_threadLocker)
            {
                _versionMetaData = new Dictionary<string, string>();
            }
            WriteVersionMetaData();
        }
    }

    private static void ReadVersionMetaData(string metaFilePath)
    {
        lock (_threadLocker)
        {
            if (_versionMetaData == null)
                _versionMetaData = new Dictionary<string, string>();
            else
                _versionMetaData.Clear();
        }
        string allText = File.ReadAllText(metaFilePath);
        string[] lines = allText.Split('\n', StringSplitOptions.None);
        foreach (string line in lines)
        {
            if (line == null) continue;
            string[] parts = line.Split('|', StringSplitOptions.None);
            if (parts.Length == 2)
            {
                lock (_threadLocker)
                {
                    _versionMetaData![parts[0]] = parts[1];
                }
            }
        }
    }

    public static void WriteVersionMetaData()
    {
        if (_versionMetaData == null) return;
        lock (_threadLocker)
        {
            foreach (KeyValuePair<string, string> kv in _versionMetaData)
                _threadStrBuilder.Append(kv.Key + "|" + kv.Value + "\n");
        }
        File.WriteAllText(Path.Combine(baseDataPath, VersionMetaDataFileName), _threadStrBuilder.ToString());
        _threadStrBuilder.Clear();
    }

    public static string GetStorageDirectoryPath(StorageRegion storageRegion = StorageRegion.Persistent, string? subDirectoryName = null)
    {
        string path;
        if (storageRegion == StorageRegion.Temporary)
            path = baseTemporaryCachePath;
        else if (storageRegion == StorageRegion.Patch)
            path = basePatchCachePath;
        else
            path = basePersistentCachePath;
        if (!string.IsNullOrEmpty(subDirectoryName))
            path = Path.Combine(path, subDirectoryName);
        return path;
    }

    public static string ConvertHTTPUriToStorageFilePath(string uriOrFileName, StorageRegion storageRegion = StorageRegion.Persistent, string? subDirectoryName = null)
    {
        int q = uriOrFileName.IndexOf('?');
        if (q != -1)
            uriOrFileName = uriOrFileName.Substring(0, q);
        string fileName = Path.GetFileName(uriOrFileName);
        return Path.Combine(GetStorageDirectoryPath(storageRegion, subDirectoryName), fileName);
    }

    public static string ConvertHTTPUriToStorageFileUri(string uriOrFileName, StorageRegion storageRegion = StorageRegion.Persistent, string? subDirectoryName = null)
    {
        return "file://" + ConvertHTTPUriToStorageFilePath(uriOrFileName, storageRegion, subDirectoryName);
    }

    private static void CreateStoreDirectory(StorageRegion storageRegion = StorageRegion.Persistent, string? subDirectoryName = null)
    {
        string path;
        if (storageRegion == StorageRegion.Temporary)
            path = baseTemporaryCachePath;
        else if (storageRegion == StorageRegion.Patch)
            path = basePatchCachePath;
        else
            path = basePersistentCachePath;

        if (!string.IsNullOrEmpty(subDirectoryName))
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = Path.Combine(path, subDirectoryName);
        }
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    public static void WriteFile(string uriOrFileName, byte[] data, string? version = null, StorageRegion storageRegion = StorageRegion.Persistent, string? subDirectoryName = null)
    {
        CreateStoreDirectory(storageRegion, subDirectoryName);
        string filePath = ConvertHTTPUriToStorageFilePath(uriOrFileName, storageRegion, subDirectoryName);
        File.WriteAllBytes(filePath, data);
        if (version != null)
            UpdateFileVersion(filePath, version);
        onFileStore?.Invoke(filePath, version);
    }

    public static bool WriteFileByMoveFile(string sourceFilePath, string uriOrFileName, string? version = null, StorageRegion storageRegion = StorageRegion.Persistent, string? subDirectoryName = null)
    {
        CreateStoreDirectory(storageRegion, subDirectoryName);
        string destPath = ConvertHTTPUriToStorageFilePath(uriOrFileName, storageRegion, subDirectoryName);
        if (File.Exists(destPath))
            File.Delete(destPath);
        File.Move(sourceFilePath, destPath);
        if (version != null)
            UpdateFileVersion(destPath, version);
        onFileStore?.Invoke(destPath, version);
        return true;
    }

    public static byte[]? ReadFile(string uriOrFileName, string? version = null, StorageRegion storageRegion = StorageRegion.Persistent, string? subDirectoryName = null)
    {
        string filePath = ConvertHTTPUriToStorageFilePath(uriOrFileName, storageRegion, subDirectoryName);
        if (!File.Exists(filePath))
            return null;
        if (version == null)
            return File.ReadAllBytes(filePath);
        if (CheckFileVersion(filePath, version))
            return File.ReadAllBytes(filePath);
        return null;
    }

    public static void DeleteFile(string uriOrFileName, StorageRegion storageRegion = StorageRegion.Persistent, string? subDirectoryName = null)
    {
        string filePath = ConvertHTTPUriToStorageFilePath(uriOrFileName, storageRegion, subDirectoryName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            UpdateFileVersion(filePath, null);
            onFileDelete?.Invoke(filePath);
        }
    }

    public static bool IsFileStored(string uriOrFileName, string? version = null, StorageRegion storageRegion = StorageRegion.Persistent, string? subDirectoryName = null)
    {
        string filePath = ConvertHTTPUriToStorageFilePath(uriOrFileName, storageRegion, subDirectoryName);
        if (!File.Exists(filePath))
            return false;
        if (version != null)
            return CheckFileVersion(filePath, version);
        return true;
    }

    public static void ClearStore(StorageRegion storageRegion, string? subDirectoryName = null)
    {
        string dirPath = GetStorageDirectoryPath(storageRegion, subDirectoryName);
        if (Directory.Exists(dirPath))
            Directory.Delete(dirPath, true);
        CreateStoreDirectory(storageRegion, subDirectoryName);
    }

    public static void ClearExpiredFiles(StorageRegion storageRegion, string? subDirectoryName = null)
    {
        string dirPath = GetStorageDirectoryPath(storageRegion, subDirectoryName);
        if (!Directory.Exists(dirPath)) return;
        foreach (string filePath in Directory.GetFiles(dirPath))
        {
            string key = GetVersionMetaDataKey(filePath);
            lock (_threadLocker)
            {
                if (_versionMetaData == null || !_versionMetaData.ContainsKey(key))
                    File.Delete(filePath);
            }
        }
    }

    public static string GetVersionMetaDataKey(string filePath)
    {
        return filePath.Substring(baseDataPath.Length);
    }

    public static void UpdateFileVersion(string filePath, string? version)
    {
        if (_versionMetaData == null) return;
        lock (_threadLocker)
        {
            string key = filePath.Substring(baseDataPath.Length);
            if (version != null)
                _versionMetaData[key] = version;
            else
                _versionMetaData.Remove(key);
            WriteVersionMetaData();
        }
    }

    public static bool CheckFileVersion(string filePath, string version)
    {
        if (_versionMetaData == null) return true;
        lock (_threadLocker)
        {
            string key = filePath.Substring(baseDataPath.Length);
            return _versionMetaData.TryGetValue(key, out string? stored) && stored == version;
        }
    }

    public static string? GetFileVersion(string uriOrFileName, StorageRegion storageRegion = StorageRegion.Persistent, string? subDirectoryName = null)
    {
        string filePath = ConvertHTTPUriToStorageFilePath(uriOrFileName, storageRegion, subDirectoryName);
        lock (_threadLocker)
        {
            if (_versionMetaData != null && _versionMetaData.TryGetValue(GetVersionMetaDataKey(filePath), out string? ver))
                return ver;
        }
        return null;
    }

    public static bool StoreData(string storeFileName, byte[] data, string? version = null, StorageRegion storageRegion = StorageRegion.Persistent, string? subDirectoryName = null, string? md5 = null, bool throwExceptionWhenDataCheckFail = false)
    {
        if (data == null) return false;
        if (!string.IsNullOrEmpty(md5) && !ValidateData(storeFileName, data, md5, throwExceptionWhenDataCheckFail))
            return false;
        WriteFile(storeFileName, data, version, storageRegion, subDirectoryName);
        return true;
    }

    public static bool ValidateData(string dataName, byte[] data, string md5, bool throwExceptionWhenDataCheckFail = false)
    {
        if (MD5_Bytes(data).Equals(md5, StringComparison.OrdinalIgnoreCase))
            return true;
        Console.WriteLine("<DownloadStore> \"" + dataName + "\" md5 check fail");
        if (throwExceptionWhenDataCheckFail)
            throw new DataCheckFail();
        return false;
    }

    public static bool ValidateFile(string uriOrFileName, string md5, StorageRegion storageRegion = StorageRegion.Persistent, string? subDirectoryName = null)
    {
        byte[]? data = ReadFile(uriOrFileName, null, storageRegion, subDirectoryName);
        if (data == null) return false;
        return ValidateData(uriOrFileName, data, md5, false);
    }

    public static string MD5_Bytes(byte[] bytes)
    {
        byte[] hash = MD5.HashData(bytes);
        StringBuilder sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public static string GetExternalResourceStoreSubDirectoryName(ResourceBundleManifest.BundleInfo bundleInfo)
    {
        if (bundleInfo.externalResourceMeta == null)
            return AssetBundleStoreSubDirectoryName;
        if (bundleInfo.externalResourceMeta.StartsWith(WwiseAudioResourceSymbol))
        {
            DecodeWwiseBundleName(bundleInfo.bundleFileName, out string? subDir);
            if (string.IsNullOrEmpty(subDir))
                return WwiseAudioStoreSubDirectoryName;
            return Path.Combine(WwiseAudioStoreSubDirectoryName, subDir);
        }
        if (bundleInfo.externalResourceMeta.StartsWith(ExternalVideoResourceSymbol))
            return ExternalVideoStoreSubDirectoryName;
        if (bundleInfo.externalResourceMeta.StartsWith(GameDataTablesSymbol))
            return GameDataTablesStoreSubDirectoryName;
        if (bundleInfo.externalResourceMeta.StartsWith(ScriptsSymbol))
            return ScriptsStoreSubDirectoryName;
        return AssetBundleStoreSubDirectoryName;
    }

    public static string GetExternalResourceStoreFileName(ResourceBundleManifest.BundleInfo bundleInfo)
    {
        if (bundleInfo.externalResourceMeta != null && bundleInfo.externalResourceMeta.StartsWith(WwiseAudioResourceSymbol))
            return DecodeWwiseBundleName(bundleInfo.bundleFileName, out _);
        return bundleInfo.bundleFileName;
    }

    public static string DecodeWwiseBundleName(string bundleName, out string? subDirectoryName)
    {
        subDirectoryName = null;
        string[] parts = bundleName.Split('.', StringSplitOptions.None);
        if (parts.Length == 3)
        {
            subDirectoryName = parts[1] switch
            {
                "cn" => "Chinese",
                "en" => "English(US)",
                "jp" => "Japanese",
                _ => null
            };
            return parts[0] + "." + parts[2];
        }
        return bundleName;
    }

    public static string EncodeWwiseBundleName(string bundleName, string subDirectoryName)
    {
        string[] parts = bundleName.Split('.', StringSplitOptions.None);
        if (parts.Length == 2)
        {
            string langCode = subDirectoryName switch
            {
                "Chinese" => "cn",
                "English(US)" => "en",
                "Japanese" => "jp",
                _ => string.Empty
            };
            if (!string.IsNullOrEmpty(langCode))
                return parts[0] + "." + langCode + "." + parts[1];
        }
        return bundleName;
    }

    public static bool IsBundleStored(ResourceBundleManifest.BundleInfo bundleInfo, string? version = null, StorageRegion storageRegion = StorageRegion.Persistent)
    {
        string subDir = GetExternalResourceStoreSubDirectoryName(bundleInfo);
        string fileName = GetExternalResourceStoreFileName(bundleInfo);
        return IsFileStored(fileName, version, storageRegion, subDir);
    }

    public static string? GetAnyStoredBundleManifestFilePath(string resourceManifestFileName, StorageRegion storageRegion = StorageRegion.Persistent)
    {
        string filePath = ConvertHTTPUriToStorageFilePath(resourceManifestFileName, storageRegion, null);
        return File.Exists(filePath) ? filePath : null;
    }

    public static string GetBundleDownloadUrl(string baseUrl, string resourceVersion, ResourceBundleManifest.BundleInfo bundleInfo)
    {
        string fileName = GetExternalResourceStoreFileName(bundleInfo);
        if (!string.IsNullOrEmpty(baseUrl))
        {
            _strBuilder.Append(baseUrl);
            if (baseUrl[^1] != '/' && baseUrl[^1] != '\\')
                _strBuilder.Append('/');
        }
        _strBuilder.Append(resourceVersion);
        _strBuilder.Append('/');
        _strBuilder.Append(fileName);
        string result = _strBuilder.ToString();
        _strBuilder.Clear();
        return result;
    }

    public static void ActiveResourceManifestTag(string tag)
    {
        ActivedResourceManifestTags.Add(tag);
    }

    public static void DeactiveResourceManifestTag(string tag)
    {
        ActivedResourceManifestTags.Remove(tag);
    }

    public static void DeactiveAllResourceManifestTags()
    {
        ActivedResourceManifestTags.Clear();
    }

    public static bool IsResourceManifestTagActived(string tag)
    {
        return ActivedResourceManifestTags.Contains(tag);
    }

    private const string TemporaryStoreDirectoryName = "Temporary_Store";
    private const string PersistentStoreDirectoryName = "Persistent_Store";
    public const string PatchStoreDirectoryName = "Patch_Store";
    public const string AssetBundleStoreSubDirectoryName = "AssetBundles";
    public const string WwiseAudioResourceSymbol = "@WWI";
    public const string WwiseAudioStoreSubDirectoryName = "SoundBanks";
    public const string ExternalVideoResourceSymbol = "@VID";
    public const string ExternalVideoStoreSubDirectoryName = "Videos";
    public const string GameDataTablesSymbol = "@TAB";
    public const string GameDataTablesStoreSubDirectoryName = "Tables";
    public const string ScriptsSymbol = "@SCR";
    public const string ScriptsStoreSubDirectoryName = "Scripts";
    public const string ExternalCompressionBundleFileExtName = ".arch";
    public const string ExternalArchiveBundleFileExtName = ".arcx";
    public const string AdditionalResourceManifestSuffixName = "_add";
    public const string DevResourceManifestSuffixName = "_dev";
    public const string InstallResourceSubDirectoryName = "InstallResource";
    public const string InstallResourceManifestFileName = "install_resource_manifest";
    private const string VersionMetaDataFileName = "file_versions.dat";
    private static string? _baseDataPath;
    private static string? _baseTemporaryStorePath;
    private static string? _basePersistentStorePath;
    private static string? _basePatchCachePath;
    private static Dictionary<string, string>? _versionMetaData = new Dictionary<string, string>();
    private static readonly object _threadLocker = new object();
    private static readonly StringBuilder _threadStrBuilder = new StringBuilder();
    private static readonly StringBuilder _strBuilder = new StringBuilder();
}
