using System.Text;
using McHoneypot.Adapters.MinecraftProtocol.Types;

namespace McHoneypot.Adapters.MinecraftProtocol.Io;

/// <summary>
/// Расширения для Stream для чтения специфичных типов протокола Minecraft.
/// </summary>
public static class MinecraftStreamExtensions
{
    extension(Stream stream)
    {
        public string ReadMinecraftString(int maxLength = 255)
        {
            int length = VarInt.ReadFrom(stream);

            if (length > maxLength * 4 || length < 0)
                throw new FormatException($"String too long: {length} bytes.");

            var stringBytes = new byte[length];
            var read = stream.Read(stringBytes, 0, length);
            return read != length
                ? throw new EndOfStreamException("Failed to read the string.")
                : Encoding.UTF8.GetString(stringBytes);
        }

        public ushort ReadBigEndianUShort()
        {
            var buffer = new byte[2];
            if (stream.Read(buffer, 0, 2) != 2)
                throw new EndOfStreamException("Unable to read ushort from stream.");

            return (ushort)((buffer[0] << 8) | buffer[1]);
        }

        public VarInt ReadVarInt()
        {
            return VarInt.ReadFrom(stream);
        }

        public void WriteVarInt(int value)
        {
            var varInt = new VarInt(value);
            varInt.WriteTo(stream);
        }

        public void WriteMinecraftString(string value)
        {
            var stringBytes = Encoding.UTF8.GetBytes(value);

            // Сначала пишем длину строки как VarInt
            stream.WriteVarInt(stringBytes.Length);

            // Затем сами байты
            stream.Write(stringBytes, 0, stringBytes.Length);
        }
    }
}