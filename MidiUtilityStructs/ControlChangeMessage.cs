using System.Runtime.InteropServices;
using Midi.Net.MidiUtilityStructs.Enums;

namespace Midi.Net.MidiUtilityStructs;

[StructLayout(LayoutKind.Explicit, Size = 2, Pack = 1)]
public readonly struct ControlChangeMessage
{
    public ControlChangeMessage(ControlChange controller, byte value)
    {
        Controller = controller;
        Value = value;
    }

    public ControlChangeMessage(byte controller, byte value)
    {
        CCNumber = controller;
        Value = value;
    }

    public ControlChangeMessage(in MidiEvent data)
    {
        CCNumber = data.DataB1;
        Value = data.DataB2;
    }

    [FieldOffset(0)] public readonly ControlChange Controller;
    [FieldOffset(0)] public readonly byte CCNumber;
    [FieldOffset(1)] public readonly byte Value;

    public void Deconstruct(out ControlChange controller, out byte value)
    {
        controller = Controller;
        value = Value;
    }

    public void Deconstruct(out byte controller, out byte value)
    {
        controller = CCNumber;
        value = Value;
    }
}