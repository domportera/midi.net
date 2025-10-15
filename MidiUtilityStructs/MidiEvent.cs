using System.Runtime.InteropServices;
using Midi.Net.MidiUtilityStructs.Enums;

namespace Midi.Net.MidiUtilityStructs;

[StructLayout(LayoutKind.Sequential, Size = 4, Pack = 1)]
// todo - change size
public readonly record struct MidiEvent
{
    public readonly MidiStatus Status;
    public readonly RawMidiData Data;

    public MidiEvent(MidiStatus status, RawMidiData data)
    {
        Status = status;
        Data = data;
    }

    public MidiEvent(MidiStatus status, ControlChangeMessage data)
    {
        Status = status;
        Data = new RawMidiData((byte)data.Controller, data.Value);
    }

    public int CopyTo(byte[] buffer, int offset)
    {
        buffer[offset] = (byte)Status;
        if (Data.Count == 0)
            return 1;

        buffer[offset + 1] = Data.B1;
        if (Data.Count == 1)
            return 2;
        
        buffer[offset + 2] = Data.B2;
        return 3;
    }

    public int CopyTo(Span<byte> buffer)
    {
        buffer[0] = (byte)Status;
        if (Data.Count == 0)
            return 1;

        buffer[1] = Data.B1;
        if (Data.Count == 1)
            return 2;
        buffer[2] = Data.B2;
        return 3;
    }

    public int Channel => Status.Channel;

    /// <summary>
    /// Returns true if the MIDI message is interpreted to be a SysEx message -
    /// a type of message that is device-specific. 
    /// </summary>
    public bool IsSystemMessage => Data.Count is 2 && Data.B2 == MidiConstants.Eox;

    public bool IsNoteOn => Status.Type == StatusType.NoteOn;

    public bool IsNoteOff => Status.Type == StatusType.NoteOff ||
                             (Status.Type == StatusType.NoteOn && Data.Count is 2 && Data.B2 == 0);
}

public readonly record struct ControlChangeMessage(ControlChange Controller, byte Value);