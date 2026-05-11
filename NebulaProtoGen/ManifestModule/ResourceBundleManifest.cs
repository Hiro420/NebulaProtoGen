using System.Globalization;
using System.Text;

namespace NebulaProtoGen;

public class ResourceBundleManifest
{
    public enum ManifestType
    {
        Unknown = 0,
        Main = 1,
        Additional = 2,
        Builtin = 3,
        BinDiffPatch = 4,
        Patch = 5
    }

    public static class VersionCompatibilityMode
    {
        public const string None = "NONE";
        public const string Target = "TARGET";
        public const string Min = "MIN";
    }

    public class BinaryDiffPatchInfo
    {
        public string? version;
        public string? patchFileSymbol;
        public string? patchFileName;
        public string? newFileTag;

        public static BinaryDiffPatchInfo? Deserialization(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;
            string[] parts = data.Split('|', StringSplitOptions.None);
            if (parts.Length < 2) return null;
            return new BinaryDiffPatchInfo
            {
                version = parts[0],
                patchFileSymbol = parts.Length > 1 ? parts[1] : null,
                patchFileName = parts.Length > 2 ? parts[2] : null,
                newFileTag = parts.Length > 3 ? parts[3] : null
            };
        }

        public static string Serialization(BinaryDiffPatchInfo info)
        {
            return string.Concat(
                info.version ?? string.Empty, "|",
                info.patchFileSymbol ?? string.Empty, "|",
                info.patchFileName ?? string.Empty, "|",
                info.newFileTag ?? string.Empty);
        }

        public override string ToString()
        {
            return $"BinDiffPatch ver={version} file={patchFileName} tag={newFileTag}";
        }
    }

    public class BundleInfo
    {
        public string bundleFileName = string.Empty;
        public string bundleHash = string.Empty;
        public long bundleSize;
        public uint bundleCrc;
        public string? externalResourceMeta;
        public string? tag;

        public bool isExternalResource => externalResourceMeta != null;

        public static BundleInfo Deserialization(string data)
        {
            BundleInfo info = new BundleInfo();
            string[] parts = data.Split('|', StringSplitOptions.None);
            if (parts.Length != 5) return info;
            info.bundleFileName = parts[0];
            info.bundleHash = parts[1];
            info.bundleSize = long.Parse(parts[2], CultureInfo.InvariantCulture);
            if (parts[3].StartsWith(ExternalResourceSymbol))
            {
                info.externalResourceMeta = parts[3];
            }
            else
            {
                info.bundleCrc = uint.Parse(parts[3], CultureInfo.InvariantCulture);
            }
            info.tag = parts[4];
            return info;
        }

        public static string Serialization(BundleInfo bundleInfo)
        {
            string field3 = bundleInfo.externalResourceMeta == null
                ? bundleInfo.bundleCrc.ToString(CultureInfo.InvariantCulture)
                : bundleInfo.externalResourceMeta;
            return string.Concat(
                bundleInfo.bundleFileName, "|",
                bundleInfo.bundleHash, "|",
                bundleInfo.bundleSize.ToString(CultureInfo.InvariantCulture), "|",
                field3, "|",
                bundleInfo.tag ?? string.Empty, "\n");
        }

        public static BundleInfo Copy(BundleInfo src)
        {
            return new BundleInfo
            {
                bundleFileName = src.bundleFileName,
                bundleHash = src.bundleHash,
                bundleSize = src.bundleSize,
                bundleCrc = src.bundleCrc,
                externalResourceMeta = src.externalResourceMeta,
                tag = src.tag
            };
        }

        public bool IsSameAs(BundleInfo other)
        {
            return bundleFileName == other.bundleFileName
                && bundleHash == other.bundleHash
                && bundleSize == other.bundleSize
                && bundleCrc == other.bundleCrc
                && externalResourceMeta == other.externalResourceMeta
                && tag == other.tag;
        }

        public override string ToString()
        {
            return string.Format("{0} Hash:{1} Size:{2:N2}MB", bundleFileName, bundleHash, (double)bundleSize / 1024.0 / 1024.0);
        }

        public static void SortBundleInfoListByFileName(List<BundleInfo> bundleList)
        {
            bundleList.Sort((a, b) => string.Compare(a.bundleFileName, b.bundleFileName, StringComparison.Ordinal));
        }

        public static void SortBundleInfoListBySize(List<BundleInfo> bundleList)
        {
            bundleList.Sort((a, b) => a.bundleSize.CompareTo(b.bundleSize));
        }
    }

    public List<string> metaList { get; private set; } = new List<string>();
    public List<BundleInfo> bundleList { get; private set; } = new List<BundleInfo>();

    public ManifestType manifestType { get; set; } = ManifestType.Unknown;
    private Dictionary<string, BundleInfo> _cacheByName = new Dictionary<string, BundleInfo>();
    private Dictionary<string, BundleInfo> _cacheByHash = new Dictionary<string, BundleInfo>();

    public static ResourceBundleManifest BinDiffPatchResourceInstance = new ResourceBundleManifest();
    public static ResourceBundleManifest PatchResourceInstance = new ResourceBundleManifest();
    public static ResourceBundleManifest BuiltinResourceInstance = new ResourceBundleManifest();
	public static readonly UTF8Encoding Encoding = new UTF8Encoding(false);

	public ResourceBundleManifest() { }

	public static ResourceBundleManifest Deserialization(string data)
    {
        ResourceBundleManifest manifest = new ResourceBundleManifest();
        manifest.metaList = new List<string>();
        manifest.bundleList = new List<BundleInfo>();
        manifest._cacheByName = new Dictionary<string, BundleInfo>();
        manifest._cacheByHash = new Dictionary<string, BundleInfo>();

        string[] lines = data.Split('\n', StringSplitOptions.None);
        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith(ManifestMetaSymbol))
            {
                manifest.metaList.Add(line);
            }
            else if (line.StartsWith(ManifestCommentSymbol))
            {
                // skip comments
            }
            else
            {
                BundleInfo info = BundleInfo.Deserialization(line);
                if (!string.IsNullOrEmpty(info.bundleFileName))
                    manifest.bundleList.Add(info);
            }
        }
        return manifest;
    }

    public static string Serialization(ResourceBundleManifest manifest)
    {
        StringBuilder sb = new StringBuilder();
        foreach (string meta in manifest.metaList)
        {
            sb.Append(meta);
            if (!meta.EndsWith('\n'))
                sb.Append('\n');
        }
        foreach (BundleInfo bundle in manifest.bundleList)
        {
            string serialized = BundleInfo.Serialization(bundle);
            if (!string.IsNullOrEmpty(serialized))
                sb.Append(serialized);
        }
        return sb.ToString();
    }

    public string? GetMetaData(string symbol)
    {
        string prefix = symbol + ":";
        foreach (string meta in metaList)
        {
            if (meta.StartsWith(prefix))
                return meta.Substring(prefix.Length);
        }
        return null;
    }

    public void SetMetaData(string symbol, string? value)
    {
        string prefix = symbol + ":";
        for (int i = 0; i < metaList.Count; i++)
        {
            if (metaList[i].StartsWith(prefix))
            {
                if (value == null)
                    metaList.RemoveAt(i);
                else
                    metaList[i] = prefix + value;
                return;
            }
        }
        if (value != null)
            metaList.Add(prefix + value);
    }

    public string? GetClientVersion() => GetMetaData(ClientVersionSymbol);
    public void SetClientVersion(string version) => SetMetaData(ClientVersionSymbol, version);

    public string? GetClientVersionCompatibilityMode() => GetMetaData(ClientVersionCompatibilityModeSymbol);
    public void SetClientVersionCompatibilityMode(string mode) => SetMetaData(ClientVersionCompatibilityModeSymbol, mode);

    public string? GetGameVersion() => GetMetaData(GameVersionSymbol);
    public void SetGameVersion(string version) => SetMetaData(GameVersionSymbol, version);

    public string? GetClearExpiredFilesValue() => GetMetaData(ClearExpiredFilesSymbol);
    public void SetClearExpiredFilesValue(string value) => SetMetaData(ClearExpiredFilesSymbol, value);

    public string? GetBinDiffPatchVersion() => GetMetaData(BinaryDiffPatchVersionSymbol);
    public void SetBinDiffPatchVersion(string version) => SetMetaData(BinaryDiffPatchVersionSymbol, version);

    public string? GetResourceVersion() => GetMetaData(ResourceVersionSymbol);
    public void SetResourceVersion(string version) => SetMetaData(ResourceVersionSymbol, version);

    public List<BinaryDiffPatchInfo> GetBinDiffPatchInfos()
    {
        List<BinaryDiffPatchInfo> result = new List<BinaryDiffPatchInfo>();
        string prefix = BinaryDiffPatchInfoSymbol + ":";
        foreach (string meta in metaList)
        {
            if (meta.StartsWith(prefix))
            {
                BinaryDiffPatchInfo? info = BinaryDiffPatchInfo.Deserialization(meta.Substring(prefix.Length));
                if (info != null) result.Add(info);
            }
        }
        return result;
    }

    public void SetBinDiffPatchInfos(List<BinaryDiffPatchInfo> infos)
    {
        string prefix = BinaryDiffPatchInfoSymbol + ":";
        metaList.RemoveAll(m => m.StartsWith(prefix));
        foreach (BinaryDiffPatchInfo info in infos)
            metaList.Add(prefix + BinaryDiffPatchInfo.Serialization(info));
    }

    public List<string> GetBinDiffPatchFiles()
    {
        List<string> result = new List<string>();
        string prefix = BinaryDiffPatchFileSymbol + ":";
        foreach (string meta in metaList)
        {
            if (meta.StartsWith(prefix))
                result.Add(meta.Substring(prefix.Length));
        }
        return result;
    }

    public void SetBinDiffPatchFiles(List<string> files)
    {
        string prefix = BinaryDiffPatchFileSymbol + ":";
        metaList.RemoveAll(m => m.StartsWith(prefix));
        foreach (string file in files)
            metaList.Add(prefix + file);
    }

    public BundleInfo? QueryBundleInfoByName(string bundleFileName)
    {
        if (_cacheByName.TryGetValue(bundleFileName, out BundleInfo? cached))
            return cached;
        foreach (BundleInfo info in bundleList)
        {
            if (info.bundleFileName == bundleFileName)
            {
                _cacheByName[bundleFileName] = info;
                return info;
            }
        }
        return null;
    }

    public BundleInfo? QueryBundleInfoByHash(string hash)
    {
        if (_cacheByHash.TryGetValue(hash, out BundleInfo? cached))
            return cached;
        foreach (BundleInfo info in bundleList)
        {
            if (info.bundleHash == hash)
            {
                _cacheByHash[hash] = info;
                return info;
            }
        }
        return null;
    }

    public List<BundleInfo> QueryBundlesInfoByTag(string tag)
    {
        List<BundleInfo> result = new List<BundleInfo>();
        foreach (BundleInfo info in bundleList)
        {
            if (info.tag == tag)
                result.Add(info);
        }
        return result;
    }

    public bool Compare(
        ResourceBundleManifest other,
        out List<BundleInfo> addList,
        out List<BundleInfo> removeList,
        out List<BundleInfo> changeList,
        out List<BundleInfo> changeListInOther,
        out List<BundleInfo> sameList)
    {
        addList = new List<BundleInfo>();
        removeList = new List<BundleInfo>();
        changeList = new List<BundleInfo>();
        changeListInOther = new List<BundleInfo>();
        sameList = new List<BundleInfo>();

        foreach (BundleInfo bundle in bundleList)
        {
            BundleInfo? otherBundle = other.QueryBundleInfoByName(bundle.bundleFileName);
            if (otherBundle == null)
            {
                addList.Add(BundleInfo.Copy(bundle));
            }
            else if (!bundle.IsSameAs(otherBundle))
            {
                changeList.Add(BundleInfo.Copy(bundle));
                changeListInOther.Add(BundleInfo.Copy(otherBundle));
            }
            else
            {
                sameList.Add(BundleInfo.Copy(bundle));
            }
        }

        foreach (BundleInfo otherBundle in other.bundleList)
        {
            if (QueryBundleInfoByName(otherBundle.bundleFileName) == null)
                removeList.Add(BundleInfo.Copy(otherBundle));
        }

        return addList.Count > 0 || removeList.Count > 0 || changeList.Count > 0;
    }

    public void Merge(ResourceBundleManifest other)
    {
        foreach (string meta in other.metaList)
        {
            if (!metaList.Contains(meta))
                metaList.Add(meta);
        }
        foreach (BundleInfo bundle in other.bundleList)
        {
            BundleInfo? existing = QueryBundleInfoByName(bundle.bundleFileName);
            if (existing == null)
            {
                bundleList.Add(BundleInfo.Copy(bundle));
                _cacheByName.Remove(bundle.bundleFileName);
            }
            else
            {
                bundleList.Remove(existing);
                _cacheByName.Remove(bundle.bundleFileName);
                bundleList.Add(BundleInfo.Copy(bundle));
            }
        }
    }

    public static string DumpCompareReport(
        List<BundleInfo> addList,
        List<BundleInfo> removeList,
        List<BundleInfo> changeList,
        List<BundleInfo> changeListInOther,
        List<BundleInfo> sameList,
        string? dumpFilePath = null)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Add List [{addList.Count}] ======");
        foreach (BundleInfo b in addList)
            sb.AppendLine(b.ToString());
        sb.AppendLine($"Remove List [{removeList.Count}] ======");
        foreach (BundleInfo b in removeList)
            sb.AppendLine(b.ToString());
        sb.AppendLine($"Change List [{changeList.Count}] ======");
        for (int i = 0; i < changeList.Count; i++)
        {
            sb.Append(changeList[i].ToString());
            sb.Append(" <-> ");
            sb.Append(changeListInOther[i].ToString());
            sb.AppendLine();
        }
        sb.AppendLine($"Same List [{sameList.Count}] ======");
        foreach (BundleInfo b in sameList)
            sb.AppendLine(b.ToString());

        string text = sb.ToString();
        if (!string.IsNullOrWhiteSpace(dumpFilePath))
            File.WriteAllText(dumpFilePath, text);
        return text;
    }

    public static string GetLocalResourceManifestFileName(string prefixName, string platformName, string additionalSuffix = "")
    {
        return prefixName.ToLower() + "_" + platformName.ToLower() + additionalSuffix;
    }

    public static string GetRemoteResourceManifestFileName(string prefixName, string platformName, string resourceVersionName, string additionalSuffix = "")
    {
        return prefixName.ToLower() + "_" + platformName.ToLower() + "_" + resourceVersionName + additionalSuffix;
    }

    public static void StoreDataToFile(ResourceBundleManifest manifest, string filePath)
    {
        byte[] bytes = Encoding.GetBytes(Serialization(manifest));
        File.WriteAllBytes(filePath, bytes);
    }

    public static void StoreDataToDownloadStore(ResourceBundleManifest manifest, string resourceManifestFileName, string? resourceVersion = null)
    {
        byte[] bytes = Encoding.GetBytes(Serialization(manifest));
        DownloadStore.WriteFile(resourceManifestFileName, bytes, resourceVersion, DownloadStore.StorageRegion.Persistent);
    }

    public static void StoreDataToDownloadStoreUseVendorPlatformName(ResourceBundleManifest manifest, string vendorPlatformName, string resourceVersion)
    {
        string fileName = GetLocalResourceManifestFileName(vendorPlatformName, "win") + ManifestFileExtName;
        StoreDataToDownloadStore(manifest, fileName, resourceVersion);
    }

    public static ResourceBundleManifest LoadDataFromFile(string filePath)
    {
        byte[] bytes = File.ReadAllBytes(filePath);
        return Deserialization(Encoding.GetString(bytes));
    }

    public static ResourceBundleManifest LoadDataFromBytes(byte[] bytes)
    {
        return Deserialization(Encoding.GetString(bytes));
    }

    public static ResourceBundleManifest? LoadDataFromDownloadStore(string resourceManifestFileName, string? resourceVersion = null)
    {
        byte[]? bytes = DownloadStore.ReadFile(resourceManifestFileName, resourceVersion, DownloadStore.StorageRegion.Persistent);
        if (bytes == null) return null;
        return Deserialization(Encoding.GetString(bytes));
    }

    public static ResourceBundleManifest? LoadDataFromDownloadStoreUseVendorPlatformName(string vendorPlatformName, string resourceVersion)
    {
        string fileName = GetLocalResourceManifestFileName(vendorPlatformName, "win") + ManifestFileExtName;
        return LoadDataFromDownloadStore(fileName, resourceVersion);
    }

    public void DeleteInDownloadStore(string resourceManifestFileName, DownloadStore.StorageRegion storageRegion = DownloadStore.StorageRegion.Persistent)
    {
        DownloadStore.DeleteFile(resourceManifestFileName, storageRegion, null);
    }

    public const string ManifestFileExtName = ".mani";
    public const string ManifestMetaSymbol = "$";
    public const string ManifestCommentSymbol = "#";
    public const string ExternalResourceSymbol = "@";
    public const string ResourceVersionSymbol = "$RES_VER";
    public const string ClientVersionSymbol = "$CLIENT_VER";
    public const string ClientVersionCompatibilityModeSymbol = "$CLIENT_VER_COMP_MODE";
    public const string GameVersionSymbol = "$GAME_VER";
    public const string ClearExpiredFilesSymbol = "$CLEAR_EXPIRED_FILES";
    public const string BinaryDiffPatchVersionSymbol = "$BIN_DIFF_PATCH_VER";
    public const string BinaryDiffPatchInfoSymbol = "$BIN_DIFF_PATCH_INFO";
    public const string BinaryDiffPatchFileSymbol = "$BIN_DIFF_PATCH_FILE";
    public const string BinaryDiffPatchNewFileTag = "N";
    public const string BinaryDiffPatchUpdateFileTag = "U";
}
