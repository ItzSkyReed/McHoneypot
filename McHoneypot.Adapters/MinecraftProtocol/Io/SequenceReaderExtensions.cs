using System;
using System.Buffers;
using System.Text;
using McHoneypot.Adapters.MinecraftProtocol.Types;

namespace McHoneypot.Adapters.MinecraftProtocol.Io;

public static class SequenceReaderExtensions
{
    public static bool TryReadMinecraftString(this ref SequenceReader<byte> reader, int maxLength, out string result)
    {
        result = string.Empty;

        if (!VarInt.TryRead(ref reader, out var length))
            return false;

        if (length > maxLength * 4 || length < 0)
            throw new InvalidOperationException($"Length ({length}) is incorrect.");

        if (reader.Remaining < length)
            return false;

        var stringSequence = reader.Sequence.Slice(reader.Position, length);

        if (stringSequence.IsSingleSegment)
        {
            result = Encoding.UTF8.GetString(stringSequence.FirstSpan);
        }
        else
        {
            // Fallback
            var rentBuffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                stringSequence.CopyTo(rentBuffer);
                result = Encoding.UTF8.GetString(rentBuffer, 0, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentBuffer);
            }
        }

        reader.Advance(length);
        return true;
    }
}