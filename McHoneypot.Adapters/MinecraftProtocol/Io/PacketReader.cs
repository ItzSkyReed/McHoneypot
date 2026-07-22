
using McHoneypot.Adapters.MinecraftProtocol.Packets;

namespace McHoneypot.Adapters.MinecraftProtocol.Io
{
    public static class PacketReader
    {
        private const int MaxPacketLength = 2097151;

        public static RawPacket ReadNextPacket(Stream stream)
        {
            int packetLength = stream.ReadVarInt();

            switch (packetLength)
            {
                case 0:
                    throw new InvalidDataException("Empty packet sent (Length = 0).");
                case > MaxPacketLength:
                    throw new InvalidDataException($"Packet too large! {packetLength} bytes claimed. Dropping connection.");
            }

            // Read the Packet ID.
            // Important: the size of the VarInt for the ID is included in the total packet length (Length)!
            // Therefore, we need to calculate how many bytes the Packet ID itself occupies.

            // Remember the position (if this is a NetworkStream, we can't use Position,
            // so we'll read the Packet ID byte by byte or use ReadVarInt,
            // but we need to know its length in bytes).

            // To avoid complicating the code by counting bytes, we'll read the entire Payload into memory.
            // PacketLength is (ID size) + (Data size).
            var packetBuffer = new byte[packetLength];
            var totalRead = 0;

            while (totalRead < packetLength)
            {
                var read = stream.Read(packetBuffer, totalRead, packetLength - totalRead);
                if (read == 0) throw new EndOfStreamException();
                totalRead += read;
            }


            using var memoryStream = new MemoryStream(packetBuffer);

            int packetId = memoryStream.ReadVarInt();

            // How many bytes are left? This is our Data
            var dataLength = (int)(memoryStream.Length - memoryStream.Position);
            var data = new byte[dataLength];
            memoryStream.ReadExactly(data, 0, dataLength);

            return new RawPacket(packetLength, packetId, data);
        }
    }
}