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
		var patches = new List<PatchEntry>();

		foreach (var entry in diff.Diffs)
		{
			var m = s_patchPattern.Match(entry.FileName);
			if (!m.Success) continue;

			string baseName = m.Groups[3].Value;
			if (!string.Equals(baseName, baseFileName, StringComparison.OrdinalIgnoreCase))
				continue;

			patches.Add(new PatchEntry(
				int.Parse(m.Groups[1].Value),
				m.Groups[2].Value,
				baseName,
				entry));
		}

		patches.Sort((a, b) => a.Index.CompareTo(b.Index));
		return patches;
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
