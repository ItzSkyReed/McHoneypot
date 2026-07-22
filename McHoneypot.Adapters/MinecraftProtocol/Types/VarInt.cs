namespace McHoneypot.Adapters.MinecraftProtocol.Types;

/// <summary>
/// Represents the VarInt variable-length type from the Minecraft protocol.
/// </summary>
public readonly struct VarInt(int value) : IEquatable<VarInt>
{
    public readonly int Value = value;

    public static implicit operator int(VarInt v) => v.Value;
    public static implicit operator VarInt(int v) => new(v);

    public static VarInt ReadFrom(Stream stream)
    {
        var numRead = 0;
        var result = 0;
        byte read;
        do
        {
            var value = stream.ReadByte();
            if (value == -1)
                throw new EndOfStreamException("Unexpected end of stream while reading VarInt.");

            read = (byte)value;
            var valuePart = read & 0x7F;
            result |= valuePart << (7 * numRead);

            numRead++;


            if (numRead > 5)
                throw new FormatException("VarInt is too long (more than 5 bytes). Attack possible.");

        } while ((read & 0x80) != 0);

        return new VarInt(result);
    }


    public void WriteTo(Stream stream)
    {
        var value = (uint)Value;
        do
        {
            var temp = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
            {
                temp |= 0x80;
            }

            stream.WriteByte(temp);
        } while (value != 0);
    }

    public static bool TryRead(ReadOnlySpan<byte> buffer, out VarInt result, out int bytesRead)
    {
        var numRead = 0;
        var res = 0;
        byte read;

        result = default;
        bytesRead = 0;

        do
        {
            if (numRead >= buffer.Length)
                return false; // Not enough data in the buffer

            read = buffer[numRead];
            var valuePart = read & 0x7F;
            res |= valuePart << (7 * numRead);

            numRead++;

            if (numRead > 5)
                throw new FormatException("VarInt exceeded the maximum length of 5 bytes.");
        } while ((read & 0x80) != 0);

        result = new VarInt(res);
        bytesRead = numRead;
        return true;
    }

    public bool TryWrite(Span<byte> buffer, out int bytesWritten)
    {
        var val = (uint)Value;
        bytesWritten = 0;

        do
        {
            if (bytesWritten >= buffer.Length)
                return false; // Buffer is too small

            var temp = (byte)(val & 0x7F);
            val >>= 7;
            if (val != 0)
            {
                temp |= 0x80;
            }

            buffer[bytesWritten] = temp;
            bytesWritten++;
        } while (val != 0);

        return true;
    }

    // For correct comparison of structures
    public bool Equals(VarInt other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is VarInt other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}