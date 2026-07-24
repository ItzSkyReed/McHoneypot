using System.Text.Json.Serialization;

namespace McHoneypot.Core.Models.Configuration;


public enum ProtocolMode
{
    Chameleon, // Dynamically adjusts to the client version
    Fixed     // Strictly responds with the specified version
}