using System.Buffers.Binary;
using System.Text;

namespace McHoneypot.Adapters.MinecraftProtocol.Io;

public ref struct PacketReader(ReadOnlySpan<byte> buffer)
{
    private readonly ReadOnlySpan<byte> _buffer = buffer;
    public int Position { get; private set; } = 0;

    public int ReadVarInt()
    {
        var numRead = 0;
        var result = 0;
        byte read;
        do
        {
            read = _buffer[Position++];
            var valuePart = read & 0x7F;
            result |= valuePart << (7 * numRead);
            numRead++;
            if (numRead > 5)
                throw new FormatException("VarInt is too long.");
        } while ((read & 0x80) != 0);

        return result;
    }

    public ushort ReadUShort()
    {
        var value = BinaryPrimitives.ReadUInt16BigEndian(_buffer.Slice(Position, 2));
        Position += 2;
        return value;
    }

    public string ReadMinecraftString(int maxLength = 255)
    {
        var length = ReadVarInt();
        if (length > maxLength * 4 || length < 0)
            throw new FormatException($"String too long: {length} bytes.");

        var stringSpan = _buffer.Slice(Position, length);
        Position += length;

        return Encoding.UTF8.GetString(stringSpan);
    }

    public long ReadLong()
    {
        var value = BinaryPrimitives.ReadInt64BigEndian(_buffer.Slice(Position, 8));
        Position += 8;
        return value;
    }
}