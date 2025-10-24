using System.Runtime.InteropServices;
using Midi.Net.MidiUtilityStructs.Enums;

namespace Midi.Net.MidiUtilityStructs;

[StructLayout(LayoutKind.Explicit, Size = 2, Pack = 1)]
public readonly record struct PolyphonicPressureMessage
{
    [FieldOffset(0)]
    public readonly NoteId Note;
    [FieldOffset(0)]
    public readonly byte NoteNumber;
    [FieldOffset(1)]
    public readonly byte Pressure;
    public float PressureNormalized => MidiParser.Value7BitNormalized(Pressure);

    public PolyphonicPressureMessage(byte dataB1, byte dataB2)
    {
        NoteNumber = dataB1;
        Pressure = dataB2;
    }
}