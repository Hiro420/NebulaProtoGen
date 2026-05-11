using System.Buffers.Binary;

namespace NebulaProtoGen;

public static class XxHash64
{
	// xxHash64 primes
	private const ulong PRIME64_1 = 0x9E3779B185EBCA87UL; // 11400714785074694791
	private const ulong PRIME64_2 = 0xC2B2AE3D27D4EB4FUL; // 14029467366897019727
	private const ulong PRIME64_3 = 0x165667B19E3779F9UL; // 1609587929392839161
	private const ulong PRIME64_4 = 0x85EBCA77C2B2AE63UL; // 9650029242287828579
	private const ulong PRIME64_5 = 0x27D4EB2F165667C5UL; // 2870177450012600261

	public static ulong Hash64(byte[] data, ulong seed = 0)
		=> Hash64(data.AsSpan(), seed);

	public static ulong Hash64(ReadOnlySpan<byte> data, ulong seed = 0)
	{
		int len = data.Length;
		int index = 0;
		ulong hash;

		if (len >= 32)
		{
			ulong v1 = seed + PRIME64_1 + PRIME64_2;
			ulong v2 = seed + PRIME64_2;
			ulong v3 = seed + 0;
			ulong v4 = seed - PRIME64_1;

			int limit = len - 32;
			while (index <= limit)
			{
				v1 = Round(v1, ReadUInt64LE(data, index + 0));
				v2 = Round(v2, ReadUInt64LE(data, index + 8));
				v3 = Round(v3, ReadUInt64LE(data, index + 16));
				v4 = Round(v4, ReadUInt64LE(data, index + 24));
				index += 32;
			}

			hash = RotateLeft(v1, 1) + RotateLeft(v2, 7)
				 + RotateLeft(v3, 12) + RotateLeft(v4, 18);

			hash = MergeRound(hash, v1);
			hash = MergeRound(hash, v2);
			hash = MergeRound(hash, v3);
			hash = MergeRound(hash, v4);
		}
		else
		{
			hash = seed + PRIME64_5;
		}

		hash += (ulong)len;

		// Process 8-byte chunks
		int eightByteLimit = len - 8;
		while (index <= eightByteLimit)
		{
			ulong k1 = ReadUInt64LE(data, index);
			k1 *= PRIME64_2;
			k1 = RotateLeft(k1, 31);
			k1 *= PRIME64_1;
			hash ^= k1;
			hash = RotateLeft(hash, 27) * PRIME64_1 + PRIME64_4;
			index += 8;
		}

		// Process 4-byte chunk
		int fourByteLimit = len - 4;
		if (index <= fourByteLimit)
		{
			hash ^= (ulong)ReadUInt32LE(data, index) * PRIME64_1;
			hash = RotateLeft(hash, 23) * PRIME64_2 + PRIME64_3;
			index += 4;
		}

		// Process remaining bytes
		while (index < len)
		{
			hash ^= (ulong)data[index] * PRIME64_5;
			hash = RotateLeft(hash, 11) * PRIME64_1;
			index++;
		}

		// Avalanche
		hash ^= hash >> 33;
		hash *= PRIME64_2;
		hash ^= hash >> 29;
		hash *= PRIME64_3;
		hash ^= hash >> 32;

		return hash;
	}

	private static ulong Round(ulong acc, ulong input)
	{
		acc += input * PRIME64_2;
		acc = RotateLeft(acc, 31);
		acc *= PRIME64_1;
		return acc;
	}

	private static ulong MergeRound(ulong acc, ulong val)
	{
		val = Round(0, val);
		acc ^= val;
		acc = acc * PRIME64_1 + PRIME64_4;
		return acc;
	}

	private static ulong RotateLeft(ulong x, int r)
		=> (x << r) | (x >> (64 - r));

	private static ulong ReadUInt64LE(ReadOnlySpan<byte> s, int offset)
		=> BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(offset, 8));

	private static uint ReadUInt32LE(ReadOnlySpan<byte> s, int offset)
		=> BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(offset, 4));
}