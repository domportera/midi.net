using System.Runtime.InteropServices;

namespace Midi.Net.MidiUtilityStructs;

[StructLayout(LayoutKind.Sequential, Size = 3, Pack = 1)]
public readonly record struct RawMidiData
{
    public readonly MidiByte B1;
    public readonly MidiByte B2;
    public readonly byte Count;

    // todo: handle Exclusive messages (p10 of the midi spec pdf, technically p5 per page numbers on the doc)

    public bool IsRealTime => Count > 0 && B1.IsStatusByte && (Count == 1 || B2.IsStatusByte);

    public RawMidiData(ReadOnlySpan<byte> bytes)
    {
        Count = (byte)bytes.Length;
        B1 = Count > 0 ? bytes[0] : (byte)0;
        B2 = Count > 1 ? bytes[1] : (byte)0;

        if (Count > 2)
            throw new ArgumentOutOfRangeException(nameof(Count), "RawMidiDataOnly can only hold up to 2 data bytes " +
                                                                 "per the midi spec.");
    }

    public RawMidiData(byte b1)
    {
        B1 = b1;
        B2 = 0;
        Count = 1;
    }

    public RawMidiData(byte b1, byte b2)
    {
        B1 = b1;
        B2 = b2;
        Count = 2;
    }
}