using System.Buffers;
using McHoneypot.Adapters.MinecraftProtocol.Types;

namespace McHoneypot.Adapters.MinecraftProtocol.Io;

public static class MinecraftPacketParser
{
    /// <summary>
    /// Attempts to extract a single complete packet from the buffer.
    /// Returns false if there is insufficient data so far (waiting for the next TCP packet).
    /// </summary>
    public static bool TryParse(
        ref ReadOnlySequence<byte> buffer,
        out int packetId,
        out ReadOnlySequence<byte> payload,
        out SequencePosition consumedTo)
    {
        packetId = 0;
        payload = default;
        consumedTo = buffer.Start;

        var reader = new SequenceReader<byte>(buffer);

        if (!VarInt.TryRead(ref reader, out var packetLength))
            return false; // VarInt длины еще не докачался

        if (reader.Remaining < packetLength)
            return false;

        var rawPacketData = reader.Sequence.Slice(reader.Position, packetLength);

        var payloadReader = new SequenceReader<byte>(rawPacketData);
        if (!VarInt.TryRead(ref payloadReader, out packetId))
        {
            throw new InvalidOperationException("Unable to read packet Id.");
        }

        payload = rawPacketData.Slice(payloadReader.Position);

        consumedTo = reader.Sequence.GetPosition(packetLength, reader.Position);

        return true;
    }
}