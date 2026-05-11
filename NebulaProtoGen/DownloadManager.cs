namespace NebulaProtoGen;

internal class DownloadManager
{
	private readonly VendorEnvData _vendor;
	private readonly HttpClient _http;
	private readonly Logger _logger;

	public DownloadManager(VendorEnvData vendor, Logger logger)
	{
		_vendor = vendor ?? throw new ArgumentNullException(nameof(vendor));
		_logger = logger;

		_http = new HttpClient(new SocketsHttpHandler
		{
			AutomaticDecompression = System.Net.DecompressionMethods.All
		})
		{
			Timeout = TimeSpan.FromMinutes(5)
		};
	}

	public (byte[] Data, long EffectiveVersion) DownloadAndProcess(
		FileDiff baseDiff,
		ClientDiff allDiffs)
	{
		var patches = PatchManager.FindPatches(allDiffs, baseDiff.FileName);
		long effectiveVersion = PatchManager.GetEffectiveVersion(baseDiff, patches);

		if (patches.Count > 0)
			_logger.Info($"Found {patches.Count} BSDIFF patch(es) for '{baseDiff.FileName}': "
				+ string.Join(", ", patches.Select(p => p.Diff.FileName)));

		_logger.Info($"Downloading '{baseDiff.FileName}' (v{baseDiff.Version}) into memory...");
		byte[] baseBytes = DownloadToBytesAsync(GetUrl(baseDiff)).GetAwaiter().GetResult();

		var patchBytesList = new List<byte[]>(patches.Count);
		foreach (var patch in patches)
		{
			_logger.Info($"Downloading patch '{patch.Diff.FileName}' (p_{patch.Index}_{patch.Type}, v{patch.Diff.Version}) into memory...");
			patchBytesList.Add(DownloadToBytesAsync(GetUrl(patch.Diff)).GetAwaiter().GetResult());
		}

		byte[] result = PatchManager.ApplyPatches(baseBytes, patchBytesList);

		if (patches.Count > 0)
			_logger.Info($"Applied {patches.Count} BSDIFF patch(es) to '{baseDiff.FileName}' in memory.");

		return (result, effectiveVersion);
	}

	private async Task<byte[]> DownloadToBytesAsync(Uri url, CancellationToken ct = default)
	{
		const int MaxRetries = 5;
		const int InitialDelayMs = 1000;

		for (int attempt = 1; attempt <= MaxRetries; attempt++)
		{
			try
			{
				using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
				resp.EnsureSuccessStatusCode();
				return await resp.Content.ReadAsByteArrayAsync(ct);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex) when (attempt < MaxRetries)
			{
				int delay = InitialDelayMs * (int)Math.Pow(2, attempt - 1);
				_logger.Info($"Attempt {attempt} failed ({ex.Message}). Retrying in {delay} ms...");
				await Task.Delay(delay, ct);
			}
		}

		throw new Exception($"Failed to download {url} after {MaxRetries} attempts.");
	}

	private Uri GetUrl(FileDiff diff)
	{
		var urlString = $"{_vendor.serverURL}/res/win/{diff.Version}/{diff.AdditionalPath}/{diff.FileName}";
		return Uri.TryCreate(urlString, UriKind.Absolute, out var uri)
			? uri
			: throw new InvalidOperationException($"Invalid URL on FileDiff: {urlString}");
	}
}
