namespace McHoneypot.Core.Models.Configuration;

public enum ProtocolMode
{
    Chameleon, // Dynamically adjusts to the client version
    Fixed, // Strictly responds with the specified version
    Random // Random protocol every time
}