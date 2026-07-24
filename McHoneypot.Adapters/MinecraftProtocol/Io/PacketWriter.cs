using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Clientbound;

namespace McHoneypot.Adapters.MinecraftProtocol.Io;

public ref struct PacketWriter(Span<byte> buffer)
{
    private readonly Span<byte> _buffer = buffer;
    public int Position { get; private set; } = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarInt(int value)
    {
        var val = (uint)value;
        do
        {
            var temp = (byte)(val & 0x7F);
            val >>= 7;
            if (val != 0)
                temp |= 0x80;
            _buffer[Position++] = temp;
        } while (val != 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLong(long value)
    {
        BinaryPrimitives.WriteInt64BigEndian(_buffer.Slice(Position, 8), value);
        Position += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetVarIntSize(int value)
    {
        var size = 0;
        var val = (uint)value;
        do
        {
            val >>= 7;
            size++;
        } while (val != 0);
        return size;
    }

    public static int GetMinecraftStringSize(string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        return GetVarIntSize(byteCount) + byteCount;
    }

    public void WriteMinecraftString(string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        WriteVarInt(byteCount);
        Encoding.UTF8.GetBytes(value, _buffer[Position..]);
        Position += byteCount;
    }

    public void WritePacketPayload(IClientboundPacket packet)
    {
        WriteVarInt(packet.PacketId);

        switch (packet)
        {
            case StatusResponsePacket statusPacket:
                WriteMinecraftString(statusPacket.JsonResponse);
                break;
            case PongResponsePacket pongPacket:
                WriteLong(pongPacket.Payload);
                break;
        }
    }
}