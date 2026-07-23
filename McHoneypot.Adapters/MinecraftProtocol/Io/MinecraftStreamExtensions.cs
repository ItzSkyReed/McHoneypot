using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using McHoneypot.Adapters.MinecraftProtocol.Types;

namespace McHoneypot.Adapters.MinecraftProtocol.Io;

/// <summary>
/// Extension for Stream to read Minecraft specific protocol.
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

            byte[]? rentedBuffer = null;
            var stringSpan = length <= 1024
                ? stackalloc byte[length]
                : (rentedBuffer = ArrayPool<byte>.Shared.Rent(length)).AsSpan(0, length);

            try
            {
                stream.ReadExactly(stringSpan);
                return Encoding.UTF8.GetString(stringSpan);
            }
            finally
            {
                if (rentedBuffer != null)
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }

        public ushort ReadUShort()
        {
            Span<byte> buffer = stackalloc byte[2];
            stream.ReadExactly(buffer);

            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        public VarInt ReadVarInt()
        {
            return VarInt.ReadFrom(stream);
        }

        public  async ValueTask<int> ReadVarIntAsync(CancellationToken cancellationToken = default)
        {
            var numRead = 0;
            var result = 0;

            var buffer = ArrayPool<byte>.Shared.Rent(1);

            try
            {
                byte read;
                do
                {
                    var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);

                    if (bytesRead == 0)
                        throw new EndOfStreamException("Unexpected end of stream while reading VarInt.");

                    read = buffer[0];
                    var valuePart = read & 0x7F;
                    result |= valuePart << (7 * numRead);
                    numRead++;

                    if (numRead > 5)
                        throw new FormatException("VarInt is too long (more than 5 bytes). Attack possible.");

                } while ((read & 0x80) != 0);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return result;
        }

        public void WriteVarInt(int value)
        {
            var varInt = new VarInt(value);
            varInt.WriteTo(stream);
        }

        public void WriteMinecraftString(string value)
        {
            var stringBytes = Encoding.UTF8.GetBytes(value);

            stream.WriteVarInt(stringBytes.Length);

            stream.Write(stringBytes, 0, stringBytes.Length);
        }

        public void WriteLong(long value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteInt64BigEndian(buffer, value);
            stream.Write(buffer);
        }
    }
}