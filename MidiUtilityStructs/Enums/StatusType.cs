namespace Midi.Net.MidiUtilityStructs.Enums;

public enum StatusType : byte
{
    None = 0x0,
    NoteOn = 0b10010000,
    NoteOff = 0b10000000,
    PolyKeyPressure = 0b10100000,
    ControlChange = 0b10110000,
    CC = ControlChange,
    ProgramChange = 0b11000000,
    ChannelPressure = 0b11010000,
    PitchBend = 0b11100000
};