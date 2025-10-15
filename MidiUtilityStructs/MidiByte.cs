using System.Runtime.InteropServices;

namespace Midi.Net.MidiUtilityStructs;

[StructLayout(LayoutKind.Explicit, Size = 1, Pack = 1)]
public readonly record struct MidiByte
{
    [FieldOffset(0)] public readonly byte Value;
    public MidiByte(byte value) => Value = value;
    public bool IsStatusByte => (Value & 0x80) == 0b10000000; // MSBit == 1 means status byte

    // implicit conversion to/from byte
    public static implicit operator byte(MidiByte midiByte) => midiByte.Value;
    public static implicit operator MidiByte(byte value) => new(value);
}