using System.Runtime.InteropServices;

namespace LinnstrumentKeyboard;

public readonly ref struct RawMidiEvent
{
    private readonly ReadOnlySpan<byte> _data;

    public RawMidiEvent(ReadOnlySpan<byte> bytes)
    {
        _data = bytes;
    }

    public bool HasStatus => (_data[0] & 0x80) == 0b10000000; // MSBit == 1 means status byte

    public MidiStatus Status =>  new(_data[0]);

    public bool IsRealTime
    {
        get
        {
            for(int i = 0; i < _data.Length; i++)
            {
                var midiByte = new MidiByte(_data[i]);
                if (!midiByte.IsStatusByte)
                    return false;
            }

            return true;
        }
    }

}

[StructLayout(LayoutKind.Explicit, Size = 1, Pack = 1)]
public readonly record struct MidiByte
{
    [FieldOffset(0)]
    public readonly byte Value;
    
    public MidiByte(byte value) => Value = value;
    
    public bool IsStatusByte => (Value & 0x80) == 0b10000000; // MSBit == 1 means status byte
    public bool IsDataByte => !IsStatusByte;
    
    // implicit conversion to/from byte
    public static implicit operator byte(MidiByte midiByte) => midiByte.Value;
    public static implicit operator MidiByte(byte value) => new(value);
}