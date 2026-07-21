using BsDiff;
using System.Text.RegularExpressions;

namespace NebulaProtoGen;

internal sealed record PatchEntry(int Index, string Type, string BaseFileName, FileDiff Diff);

internal static class PatchManager
{
	private static readonly Regex s_patchPattern =
		new(@"^p_(\d+)_([a-zA-Z]+)\.(.+)$", RegexOptions.Compiled);

	public static List<PatchEntry> FindPatches(ClientDiff diff, string baseFileName)
	{
		var baseDiff = diff.Diffs.FirstOrDefault(entry =>
			string.Equals(entry.FileName, baseFileName, StringComparison.OrdinalIgnoreCase));

		if (baseDiff is null)
			return [];

		var patches = new List<PatchEntry>();

		foreach (var entry in diff.Diffs)
		{
			var match = s_patchPattern.Match(entry.FileName);
			if (!match.Success)
				continue;

			string patchBaseName = match.Groups[3].Value;
			if (!string.Equals(patchBaseName, baseFileName, StringComparison.OrdinalIgnoreCase))
				continue;

			if (!int.TryParse(match.Groups[1].Value, out int index))
				continue;

			patches.Add(new PatchEntry(
				index,
				match.Groups[2].Value,
				patchBaseName,
				entry));
		}

		patches.Sort((a, b) => a.Index.CompareTo(b.Index));

		var validPatches = new List<PatchEntry>();
		long currentVersion = baseDiff.Version;

		foreach (var patch in patches)
		{
			if (patch.Diff.Version <= currentVersion)
				break;

			validPatches.Add(patch);
			currentVersion = patch.Diff.Version;
		}

		return validPatches;
	}

	public static long GetEffectiveVersion(FileDiff baseDiff, IReadOnlyList<PatchEntry> patches)
		=> patches.Count > 0 ? patches[^1].Diff.Version : baseDiff.Version;

	public static byte[] ApplyPatches(byte[] baseBytes, IReadOnlyList<byte[]> orderedPatchBytes)
	{
		if (baseBytes == null)
			throw new ArgumentNullException(nameof(baseBytes));

		if (orderedPatchBytes == null)
			throw new ArgumentNullException(nameof(orderedPatchBytes));

		byte[] current = baseBytes;

		for (int i = 0; i < orderedPatchBytes.Count; i++)
		{
			byte[] patchBytes = orderedPatchBytes[i];

			if (patchBytes == null)
				throw new ArgumentException($"Patch at index {i} is null.", nameof(orderedPatchBytes));

			using var input = new MemoryStream(current, writable: false);
			using var output = new MemoryStream();

			BinaryPatch.Apply(
				input,
				openPatchStream: () => new MemoryStream(patchBytes, writable: false),
				output);

			current = output.ToArray();
		}

		return current;
	}
}
