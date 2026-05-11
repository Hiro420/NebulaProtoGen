using K4os.Compression.LZ4;
using System.Text;

namespace NebulaProtoGen;

using u32 = UInt32;
using u64 = UInt64;

class ArcxExtract
{
	private static readonly byte[] XorKey101 =
		Encoding.ASCII.GetBytes("&^^%#$#_$!@![]<_>?GHBFR_1153SDR_");
	private static readonly byte[] XorKey102 =
		Encoding.ASCII.GetBytes("&^^%#$#_$!@![]<_>?GHBFR_7481SDR_");

	public static List<ArcxEntry> ParseData(byte[] data)
	{
		using var ms = new MemoryStream(data, writable: false);
		using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

		List<ArcxEntry> ret = new List<ArcxEntry>();
		ArchiveHeader101 header = ArchiveHeader101.ReadFrom(reader);

		if (header.Magic != 0x5241421A)
			throw new InvalidDataException($"Bad magic: 0x{header.Magic:X8}");

		switch (header.Version)
		{
			case 101:
				ret = ReadV101(data);
				break;
			case 102:
				ret = ReadV102(data);
				break;
			default:
				throw new InvalidDataException($"Unexpected version: {header.Version}");
		}
		return ret;
	}

	private static List<ArcxEntry> ReadV101(byte[] data)
	{
		using var ms = new MemoryStream(data, writable: false);
		using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);
		ArchiveHeader101 header = ArchiveHeader101.ReadFrom(reader);

		List<ArcxEntry> ret = new List<ArcxEntry>();

		if (header.Padding != 0)
			throw new InvalidDataException($"Padding must be 0, got {header.Padding}");

		var headerData = reader.ReadBytes(checked((int)header.HeaderCompressedSize));
		if ((header.HeaderFlag & 0x10) != 0)
			headerData = XorRepeat(headerData, XorKey101);

		ArchiveEntry[] entries;
		using (var hdrMs = new MemoryStream(headerData, writable: false))
		using (var hdrReader = new BinaryReader(hdrMs, Encoding.UTF8, leaveOpen: true))
		{
			entries = new ArchiveEntry[header.EntryCount];
			for (int i = 0; i < entries.Length; i++)
				entries[i] = ArchiveEntry.ReadFrom(hdrReader);
		}

		bool blocksEncrypted = (header.BlockFlag & 0x100) != 0;
		bool blocksCompressed = (header.BlockFlag & 0x10) != 0;

		foreach (var entry in entries)
		{
			long offset = (long)entry.BlockOffset << 12; // block_offset << 12
			ms.Position = offset;

			var compSize = checked((int)entry.CompressedSize);
			byte[] entryData = reader.ReadBytes(compSize);

			if (blocksEncrypted)
				entryData = XorRepeat(entryData, XorKey101);

			if (blocksCompressed)
			{
				var decompSize = checked((int)entry.DecompressedSize);
				var dest = new byte[decompSize];
				int decoded = LZ4Codec.Decode(
					entryData, 0, entryData.Length,
					dest, 0, dest.Length);

				if (decoded != decompSize)
					throw new InvalidDataException(
						$"LZ4 decoded {decoded} bytes, expected {decompSize}");

				entryData = dest;

				ret.Add(new ArcxEntry(entry.Hash, entryData));
			}
		}

		return ret;
	}

	private static List<ArcxEntry> ReadV102(byte[] data)
	{
		using var ms = new MemoryStream(data, writable: false);
		using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

		ArchiveHeader102 header = ArchiveHeader102.ReadFrom(reader);

		List<ArcxEntry> ret = new List<ArcxEntry>();

		// Read header block
		byte[] headerData = reader.ReadBytes(checked((int)header.HeaderCompressedSize));

		// Header XOR
		if ((header.Flag & 0x10) != 0)
			headerData = XorRepeat(headerData, XorKey102);

		// Header LZ4 decompress
		if ((header.Flag & 0x1) != 0)
		{
			int decompSize = checked((int)header.HeaderDecompressedSize); // or whatever the uncompressed header size field is called
			byte[] dest = new byte[decompSize];

			int decoded = LZ4Codec.Decode(
				headerData, 0, headerData.Length,
				dest, 0, dest.Length);

			if (decoded != decompSize)
				throw new InvalidDataException(
					$"Header LZ4 decoded {decoded} bytes, expected {decompSize}");

			headerData = dest;
		}

		// Parse entries from header
		ArchiveEntry[] entries;
		using (var hdrMs = new MemoryStream(headerData, writable: false))
		using (var hdrReader = new BinaryReader(hdrMs, Encoding.UTF8, leaveOpen: true))
		{
			entries = new ArchiveEntry[header.EntryCount];
			for (int i = 0; i < entries.Length; i++)
				entries[i] = ArchiveEntry.ReadFrom(hdrReader);
		}

		bool blocksEncrypted = (header.Flag & 0x1000) != 0;
		bool blocksCompressed = (header.Flag & 0x100) != 0;

		// Read each entry's data
		foreach (var entry in entries)
		{
			long offset = (long)entry.BlockOffset; // block_offset << 12
			ms.Position = offset;

			int compSize = checked((int)entry.CompressedSize);
			byte[] entryData = reader.ReadBytes(compSize);

			// Block XOR
			if (blocksEncrypted)
				entryData = XorRepeat(entryData, XorKey102);

			// Block LZ4 decompress
			if (blocksCompressed)
			{
				int decompSize = checked((int)entry.DecompressedSize);
				byte[] dest = new byte[decompSize];

				int decoded = LZ4Codec.Decode(
					entryData, 0, entryData.Length,
					dest, 0, dest.Length);

				if (decoded != decompSize)
					throw new InvalidDataException(
						$"LZ4 decoded {decoded} bytes, expected {decompSize}");

				entryData = dest;
			}

			// Aand finally
			ret.Add(new ArcxEntry(entry.Hash, entryData));
		}

		return ret;
	}

	private static byte[] XorRepeat(byte[] data, byte[] key)
	{
		byte[] result = new byte[data.Length];
		for (int i = 0; i < data.Length; i++)
			result[i] = (byte)(data[i] ^ key[i % key.Length]);
		return result;
	}
}

// Different header formats for v1.0.1 and v1.0.2, but the entry format is the same

struct ArchiveHeader101
{
	public u32 Magic;
	public u32 Version;
	public u32 HeaderFlag;
	public u32 BlockFlag;
	public u32 HeaderDecompressedSize;
	public u32 HeaderCompressedSize;
	public u32 EntryCount;
	public u32 Padding;

	public static ArchiveHeader101 ReadFrom(BinaryReader br)
	{
		return new ArchiveHeader101
		{
			Magic = br.ReadUInt32(),
			Version = br.ReadUInt32(),
			HeaderFlag = br.ReadUInt32(),
			BlockFlag = br.ReadUInt32(),
			HeaderDecompressedSize = br.ReadUInt32(),
			HeaderCompressedSize = br.ReadUInt32(),
			EntryCount = br.ReadUInt32(),
			Padding = br.ReadUInt32(),
		};
	}
}

struct ArchiveHeader102
{
	public u32 Magic;
	public u32 Version;
	public u32 Flag;
	public u32 HeaderDecompressedSize;
	public u32 HeaderCompressedSize;
	public u32 EntryCount;

	public static ArchiveHeader102 ReadFrom(BinaryReader br)
	{
		return new ArchiveHeader102
		{
			Magic = br.ReadUInt32(),
			Version = br.ReadUInt32(),
			Flag = br.ReadUInt32(),
			HeaderDecompressedSize = br.ReadUInt32(),
			HeaderCompressedSize = br.ReadUInt32(),
			EntryCount = br.ReadUInt32(),
		};
	}
}

struct ArchiveEntry
{
	public u64 Hash;
	public u32 BlockOffset;
	public u32 DecompressedSize;
	public u32 CompressedSize;

	public static ArchiveEntry ReadFrom(BinaryReader br)
	{
		return new ArchiveEntry
		{
			Hash = br.ReadUInt64(),
			BlockOffset = br.ReadUInt32(),
			DecompressedSize = br.ReadUInt32(),
			CompressedSize = br.ReadUInt32(),
		};
	}
}

public class ArcxEntry
{
	public ulong hash;
	public byte[] data = [];

	public ArcxEntry(ulong _hash, byte[] _data)
	{
		hash = _hash;
		data = _data;
	}
}