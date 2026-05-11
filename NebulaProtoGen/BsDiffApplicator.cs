using ICSharpCode.SharpZipLib.BZip2;
using System.Buffers;
using System.Runtime.InteropServices;

namespace BsDiff;

public static class BinaryPatch
{
	private const long c_fileSignature = 3473478480300364610L; // "BSDIFF40"
	private const int c_headerSize = 32;

	public static void Create(byte[] oldData, byte[] newData, Stream output)
	{
		if (oldData == null)
			throw new ArgumentNullException(nameof(oldData));

		if (newData == null)
			throw new ArgumentNullException(nameof(newData));

		Create(oldData.AsSpan(), newData.AsSpan(), output);
	}

	public static void Create(ReadOnlySpan<byte> oldData, ReadOnlySpan<byte> newData, Stream output)
	{
		if (output == null)
			throw new ArgumentNullException(nameof(output));

		if (!output.CanSeek)
			throw new ArgumentException("Output stream must be seekable.", nameof(output));

		if (!output.CanWrite)
			throw new ArgumentException("Output stream must be writable.", nameof(output));

		Span<byte> header = stackalloc byte[c_headerSize];
		WriteInt64(header.Slice(0, 8), c_fileSignature);
		WriteInt64(header.Slice(24, 8), newData.Length);

		long headerPosition = output.Position;
		output.Write(header);

		int[] suffixArray = SuffixSort(oldData);
		byte[] diffBlock = new byte[newData.Length];
		byte[] extraBlock = new byte[newData.Length];
		byte[] controlBuffer = new byte[8];

		int diffLength = 0;
		int extraLength = 0;

		long controlStart = output.Position;

		using (BZip2OutputStream controlStream = new BZip2OutputStream(output))
		{
			controlStream.IsStreamOwner = false;

			int scan = 0;
			int matchLength = 0;
			int lastScan = 0;
			int lastPos = 0;
			int lastOffset = 0;

			while (scan < newData.Length)
			{
				int oldScore = 0;
				int scanStart;

				for (scan += matchLength, scanStart = scan; scan < newData.Length; scan++)
				{
					matchLength = Search(
						suffixArray,
						oldData,
						newData,
						scan,
						0,
						oldData.Length,
						out int pos);

					while (scanStart < scan + matchLength)
					{
						if (scanStart + lastOffset < oldData.Length &&
							oldData[scanStart + lastOffset] == newData[scanStart])
						{
							oldScore++;
						}

						scanStart++;
					}

					if ((matchLength == oldScore && matchLength != 0) ||
						matchLength > oldScore + 8)
					{
						break;
					}

					if (scan + lastOffset < oldData.Length &&
						oldData[scan + lastOffset] == newData[scan])
					{
						oldScore--;
					}
				}

				if (matchLength == oldScore && scan != newData.Length)
					continue;

				Search(
					suffixArray,
					oldData,
					newData,
					scan,
					0,
					oldData.Length,
					out int currentPos);

				int forwardScore = 0;
				int bestForwardScore = 0;
				int forwardLength = 0;

				for (int i = 0;
					 lastScan + i < scan && lastPos + i < oldData.Length;
					 i++)
				{
					if (oldData[lastPos + i] == newData[lastScan + i])
						forwardScore++;

					if (2 * forwardScore - i > 2 * bestForwardScore - forwardLength)
					{
						bestForwardScore = forwardScore;
						forwardLength = i + 1;
					}
				}

				int backwardLength = 0;

				if (scan < newData.Length)
				{
					int backwardScore = 0;
					int bestBackwardScore = 0;

					for (int i = 1; scan >= lastScan + i && currentPos >= i; i++)
					{
						if (oldData[currentPos - i] == newData[scan - i])
							backwardScore++;

						if (2 * backwardScore - i > 2 * bestBackwardScore - backwardLength)
						{
							bestBackwardScore = backwardScore;
							backwardLength = i;
						}
					}
				}

				if (lastScan + forwardLength > scan - backwardLength)
				{
					int overlap = lastScan + forwardLength - (scan - backwardLength);
					int score = 0;
					int bestScore = 0;
					int overlapLength = 0;

					for (int i = 0; i < overlap; i++)
					{
						if (newData[lastScan + forwardLength - overlap + i] ==
							oldData[lastPos + forwardLength - overlap + i])
						{
							score++;
						}

						if (newData[scan - backwardLength + i] ==
							oldData[currentPos - backwardLength + i])
						{
							score--;
						}

						if (score > bestScore)
						{
							bestScore = score;
							overlapLength = i + 1;
						}
					}

					forwardLength += overlapLength - overlap;
					backwardLength -= overlapLength;
				}

				for (int i = 0; i < forwardLength; i++)
				{
					diffBlock[diffLength + i] =
						unchecked((byte)(newData[lastScan + i] - oldData[lastPos + i]));
				}

				int extraBlockLength = (scan - backwardLength) - (lastScan + forwardLength);

				for (int i = 0; i < extraBlockLength; i++)
					extraBlock[extraLength + i] = newData[lastScan + forwardLength + i];

				diffLength += forwardLength;
				extraLength += extraBlockLength;

				WriteInt64(controlBuffer.AsSpan(), forwardLength);
				controlStream.Write(controlBuffer, 0, controlBuffer.Length);

				WriteInt64(controlBuffer.AsSpan(), extraBlockLength);
				controlStream.Write(controlBuffer, 0, controlBuffer.Length);

				WriteInt64(controlBuffer.AsSpan(), (currentPos - backwardLength) - (lastPos + forwardLength));
				controlStream.Write(controlBuffer, 0, controlBuffer.Length);

				lastScan = scan - backwardLength;
				lastPos = currentPos - backwardLength;
				lastOffset = currentPos - scan;
			}
		}

		long diffStart = output.Position;

		using (BZip2OutputStream diffStream = new BZip2OutputStream(output))
		{
			diffStream.IsStreamOwner = false;
			diffStream.Write(diffBlock, 0, diffLength);
		}

		long extraStart = output.Position;

		using (BZip2OutputStream extraStream = new BZip2OutputStream(output))
		{
			extraStream.IsStreamOwner = false;
			extraStream.Write(extraBlock, 0, extraLength);
		}

		long endPosition = output.Position;

		WriteInt64(header.Slice(8, 8), diffStart - controlStart);
		WriteInt64(header.Slice(16, 8), extraStart - diffStart);

		output.Position = headerPosition;
		output.Write(header);
		output.Position = endPosition;
	}

	public static void Apply(Stream input, Func<Stream> openPatchStream, Stream output)
	{
		if (input == null)
			throw new ArgumentNullException(nameof(input));

		if (openPatchStream == null)
			throw new ArgumentNullException(nameof(openPatchStream));

		if (output == null)
			throw new ArgumentNullException(nameof(output));

		long controlLength;
		long diffLength;
		long newSize;

		using (Stream patchStream = openPatchStream())
		{
			if (!patchStream.CanRead)
				throw new ArgumentException("Patch stream must be readable.", nameof(openPatchStream));

			if (!patchStream.CanSeek)
				throw new ArgumentException("Patch stream must be seekable.", nameof(openPatchStream));

			Span<byte> header = stackalloc byte[c_headerSize];
			patchStream.ReadExactly(header);

			if (ReadInt64(header.Slice(0, 8)) != c_fileSignature)
				throw new InvalidOperationException("Corrupt patch.");

			controlLength = ReadInt64(header.Slice(8, 8));
			diffLength = ReadInt64(header.Slice(16, 8));
			newSize = ReadInt64(header.Slice(24, 8));

			if (controlLength < 0 || diffLength < 0 || newSize < 0)
				throw new InvalidOperationException("Corrupt patch.");
		}

		byte[] oldBuffer = new byte[0x100000];
		byte[] newBuffer = new byte[0x100000];

		Stream controlPatchStream = openPatchStream();
		Stream diffPatchStream = openPatchStream();
		Stream extraPatchStream = openPatchStream();

		controlPatchStream.Seek(c_headerSize, SeekOrigin.Begin);
		diffPatchStream.Seek(c_headerSize + controlLength, SeekOrigin.Begin);
		extraPatchStream.Seek(c_headerSize + controlLength + diffLength, SeekOrigin.Begin);

		using (controlPatchStream)
		using (diffPatchStream)
		using (extraPatchStream)
		using (BZip2InputStream controlStream = new BZip2InputStream(controlPatchStream))
		using (BZip2InputStream diffStream = new BZip2InputStream(diffPatchStream))
		using (BZip2InputStream extraStream = new BZip2InputStream(extraPatchStream))
		{
			long[] control = new long[3];
			byte[] controlBuffer = new byte[8];

			long oldPosition = 0;
			long newPosition = 0;

			while (newPosition < newSize)
			{
				for (int i = 0; i < 3; i++)
				{
					controlStream.ReadExactly(controlBuffer, 0, controlBuffer.Length);
					control[i] = ReadInt64(controlBuffer);
				}

				if (control[0] < 0 || control[1] < 0)
					throw new InvalidOperationException("Corrupt patch.");

				if (newPosition + control[0] > newSize)
					throw new InvalidOperationException("Corrupt patch.");

				input.Position = oldPosition;

				long remaining = control[0];

				while (remaining > 0)
				{
					int count = (int)Math.Min(remaining, newBuffer.Length);

					diffStream.ReadExactly(newBuffer, 0, count);

					int read = input.Read(oldBuffer, 0, count);
					for (int i = read; i < count; i++)
						oldBuffer[i] = 0;

					for (int i = 0; i < count; i++)
						newBuffer[i] = unchecked((byte)(newBuffer[i] + oldBuffer[i]));

					output.Write(newBuffer, 0, count);

					newPosition += count;
					oldPosition += count;
					remaining -= count;
				}

				if (newPosition + control[1] > newSize)
					throw new InvalidOperationException("Corrupt patch.");

				remaining = control[1];

				while (remaining > 0)
				{
					int count = (int)Math.Min(remaining, newBuffer.Length);

					extraStream.ReadExactly(newBuffer, 0, count);
					output.Write(newBuffer, 0, count);

					newPosition += count;
					remaining -= count;
				}

				oldPosition += control[2];
			}
		}
	}

	private static int CompareBytes(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
	{
		int length = Math.Min(left.Length, right.Length);
		return left.Slice(0, length).SequenceCompareTo(right.Slice(0, length));
	}

	private static int Search(
		int[] I,
		ReadOnlySpan<byte> oldData,
		ReadOnlySpan<byte> newData,
		int newOffset,
		int start,
		int end,
		out int pos)
	{
		if (end - start < 2)
		{
			int x = oldData.Slice(I[start]).CommonPrefixLength(newData.Slice(newOffset));
			int y = oldData.Slice(I[end]).CommonPrefixLength(newData.Slice(newOffset));

			if (x > y)
			{
				pos = I[start];
				return x;
			}

			pos = I[end];
			return y;
		}

		int mid = start + (end - start) / 2;

		if (CompareBytes(oldData.Slice(I[mid]), newData.Slice(newOffset)) < 0)
			return Search(I, oldData, newData, newOffset, mid, end, out pos);

		return Search(I, oldData, newData, newOffset, start, mid, out pos);
	}

	private static void Split(int[] I, int[] v, int start, int len, int h)
	{
		static void Swap(ref int first, ref int second)
		{
			int temp = second;
			second = first;
			first = temp;
		}

		if (len < 16)
		{
			for (int k = start; k < start + len;)
			{
				int j = 1;
				int x = v[I[k] + h];

				for (int i = 1; k + i < start + len; i++)
				{
					if (v[I[k + i] + h] < x)
					{
						x = v[I[k + i] + h];
						j = 0;
					}

					if (v[I[k + i] + h] == x)
					{
						Swap(ref I[k + j], ref I[k + i]);
						j++;
					}
				}

				for (int i = 0; i < j; i++)
					v[I[k + i]] = k + j - 1;

				if (j == 1)
					I[k] = -1;

				k += j;
			}

			return;
		}

		int pivot = v[I[start + len / 2] + h];

		int lessCount = 0;
		int equalCount = 0;

		for (int i = start; i < start + len; i++)
		{
			if (v[I[i] + h] < pivot)
				lessCount++;

			if (v[I[i] + h] == pivot)
				equalCount++;
		}

		int lessEnd = start + lessCount;
		int equalEnd = lessEnd + equalCount;

		int lessEqualMoved = 0;
		int greaterMoved = 0;

		for (int i = start; i < lessEnd;)
		{
			int value = v[I[i] + h];

			if (value < pivot)
			{
				i++;
			}
			else if (value == pivot)
			{
				Swap(ref I[i], ref I[lessEnd + lessEqualMoved]);
				lessEqualMoved++;
			}
			else
			{
				Swap(ref I[i], ref I[equalEnd + greaterMoved]);
				greaterMoved++;
			}
		}

		while (lessEnd + lessEqualMoved < equalEnd)
		{
			if (v[I[lessEnd + lessEqualMoved] + h] == pivot)
			{
				lessEqualMoved++;
			}
			else
			{
				Swap(ref I[lessEnd + lessEqualMoved], ref I[equalEnd + greaterMoved]);
				greaterMoved++;
			}
		}

		if (lessEnd > start)
			Split(I, v, start, lessEnd - start, h);

		for (int i = 0; i < equalEnd - lessEnd; i++)
			v[I[lessEnd + i]] = equalEnd - 1;

		if (lessEnd == equalEnd - 1)
			I[lessEnd] = -1;

		if (start + len > equalEnd)
			Split(I, v, equalEnd, start + len - equalEnd, h);
	}

	private static int[] SuffixSort(ReadOnlySpan<byte> oldData)
	{
		int oldSize = oldData.Length;
		int[] buckets = new int[256];

		for (int i = 0; i < oldSize; i++)
			buckets[oldData[i]]++;

		for (int i = 1; i < 256; i++)
			buckets[i] += buckets[i - 1];

		for (int i = 255; i > 0; i--)
			buckets[i] = buckets[i - 1];

		buckets[0] = 0;

		int[] I = new int[oldSize + 1];
		int[] v = new int[oldSize + 1];

		for (int i = 0; i < oldSize; i++)
			I[++buckets[oldData[i]]] = i;

		for (int i = 0; i < oldSize; i++)
			v[i] = buckets[oldData[i]];

		for (int i = 1; i < 256; i++)
		{
			if (buckets[i] == buckets[i - 1] + 1)
				I[buckets[i]] = -1;
		}

		I[0] = -1;

		for (int h = 1; I[0] != -(oldSize + 1); h += h)
		{
			int len = 0;

			for (int i = 0; i < oldSize + 1;)
			{
				if (I[i] < 0)
				{
					len -= I[i];
					i -= I[i];
				}
				else
				{
					if (len != 0)
						I[i - len] = -len;

					len = v[I[i]] + 1 - i;
					Split(I, v, i, len, h);
					i += len;
					len = 0;
				}
			}

			if (len != 0)
				I[oldSize + 1 - len] = -len;
		}

		for (int i = 0; i < oldSize + 1; i++)
			I[v[i]] = i;

		return I;
	}

	internal static long ReadInt64(ReadOnlySpan<byte> buffer)
	{
		ulong value = MemoryMarshal.Read<ulong>(buffer);
		ulong magnitude = value & 0x7FFFFFFFFFFFFFFFUL;

		if ((value & 0x8000000000000000UL) != 0)
			return -(long)magnitude;

		return (long)magnitude;
	}

	internal static void WriteInt64(Span<byte> buffer, long value)
	{
		long sign = value >> 63;
		ulong magnitude = (ulong)(sign ^ (sign + value));
		ulong encoded = ((ulong)value & 0x8000000000000000UL) | magnitude;

		MemoryMarshal.Write(buffer, in encoded);
	}
}

internal static class MemoryExtensions
{
	public static int CommonPrefixLength(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> other)
	{
		int length = Math.Min(span.Length, other.Length);

		for (int i = 0; i < length; i++)
		{
			if (span[i] != other[i])
				return i;
		}

		return length;
	}
}

internal static class StreamExtensions
{
	public static void ReadExactly(this Stream stream, Span<byte> buffer)
	{
		byte[] rented = ArrayPool<byte>.Shared.Rent(buffer.Length);

		try
		{
			stream.ReadExactly(rented, 0, buffer.Length);
			rented.AsSpan(0, buffer.Length).CopyTo(buffer);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(rented);
		}
	}

	public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int length)
	{
		for (int totalRead = 0; totalRead < length;)
		{
			int read = stream.Read(buffer, offset + totalRead, length - totalRead);

			if (read == 0)
				throw new EndOfStreamException();

			totalRead += read;
		}
	}

	public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
	{
		byte[] rented = ArrayPool<byte>.Shared.Rent(buffer.Length);

		try
		{
			buffer.CopyTo(rented.AsSpan(0, buffer.Length));
			stream.Write(rented, 0, buffer.Length);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(rented);
		}
	}
}