using System.Buffers;
using McHoneypot.Adapters.MinecraftProtocol.Packets;
using McHoneypot.Adapters.MinecraftProtocol.Packets.Clientbound;
using McHoneypot.Adapters.MinecraftProtocol.Types;

namespace McHoneypot.Adapters.MinecraftProtocol.Io;

public static class PacketWriter
{
    public static void SendPacket(Stream networkStream, IClientboundPacket packet)
    {
        using var payloadStream = new MemoryStream();
        payloadStream.WriteVarInt(packet.PacketId);
        switch (packet)
        {
            case StatusResponsePacket statusPacket:
                payloadStream.WriteMinecraftString(statusPacket.JsonResponse);
                break;
            case PongResponsePacket pongPacket:
                payloadStream.WriteLong(pongPacket.Payload);
                break;
        }

        ReadOnlySpan<byte> payloadSpan = payloadStream.GetBuffer().AsSpan(0, (int)payloadStream.Length);

        networkStream.WriteVarInt(payloadSpan.Length);
        networkStream.Write(payloadSpan);
        networkStream.Flush();
    }

    public static async Task SendPacketAsync(Stream networkStream, IClientboundPacket packet, CancellationToken cancellationToken = default)
    {
        using var payloadStream = new MemoryStream();
        payloadStream.WriteVarInt(packet.PacketId);

        switch (packet)
        {
            case StatusResponsePacket statusPacket:
                payloadStream.WriteMinecraftString(statusPacket.JsonResponse);
                break;
            case PongResponsePacket pongPacket:
                payloadStream.WriteLong(pongPacket.Payload);
                break;
        }

        var payloadBuffer = payloadStream.GetBuffer();
        var payloadLength = (int)payloadStream.Length;


        var lengthBuffer = ArrayPool<byte>.Shared.Rent(5);
        try
        {
            var packetLengthVarInt = new VarInt(payloadLength);
            packetLengthVarInt.TryWrite(lengthBuffer, out var lengthBytesWritten);

            await networkStream.WriteAsync(lengthBuffer.AsMemory(0, lengthBytesWritten), cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(lengthBuffer);
        }

        await networkStream.WriteAsync(payloadBuffer.AsMemory(0, payloadLength), cancellationToken);

        await networkStream.FlushAsync(cancellationToken);
    }
}