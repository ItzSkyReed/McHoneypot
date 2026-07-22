namespace McHoneypot.Adapters.MinecraftProtocol.Packets.Clientbound;

public class StatusResponsePacket(string jsonResponse) : IClientboundPacket
{
    public int PacketId => 0x00;

    public string JsonResponse { get; set; } = jsonResponse;
}