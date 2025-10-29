using System.Runtime.InteropServices;
using Midi.Net.MidiUtilityStructs.Enums;

namespace Midi.Net.MidiUtilityStructs;

[StructLayout(LayoutKind.Explicit)]
// todo - change size
public readonly record struct MidiEvent
{
    [FieldOffset(0)]
    public readonly MidiStatus Status;
    [FieldOffset(1)]
    public readonly ushort Data;
    [FieldOffset(1)]
    public readonly byte DataB1OrMsb;
    [FieldOffset(2)]
    public readonly byte DataB2OrLsb;

    public MidiEvent(MidiStatus status, byte data1, byte data2)
    {
        Status = status;
        DataB1OrMsb = data1;
        DataB2OrLsb = data2;
    }

    public MidiEvent(byte channel, ControlChangeMessage data)
    {
        Status = new MidiStatus(StatusType.ControlChange, channel);
        DataB1OrMsb = (byte)data.Controller;
        DataB2OrLsb = data.Value;
    }

    public MidiEvent(byte channel, MidiNoteMessage noteMessage)
    {
        Status = new MidiStatus(noteMessage.Velocity == 0 ? StatusType.NoteOff : StatusType.NoteOn, channel);
        DataB1OrMsb = (byte)noteMessage.NoteId;
        DataB2OrLsb = noteMessage.Velocity;
    }

    public int CopyTo(byte[] buffer, int offset)
    {
        buffer[offset++] = (byte)Status;
        buffer[offset++] = DataB1OrMsb;
        buffer[offset] = DataB2OrLsb;
        return 3;
    }

    public int CopyTo(Span<byte> buffer)
    {
        buffer[0] = (byte)Status;
        buffer[1] = DataB1OrMsb;
        buffer[2] = DataB2OrLsb;
        return 3;
    }

    public int Channel => Status.Channel;

    /// <summary>
    /// Returns true if the MIDI message is interpreted to be a SysEx message -
    /// a type of message that is device-specific. 
    /// </summary>
    public bool IsSystemMessage => DataB2OrLsb == MidiConstants.Eox;

    public bool IsNoteOn => Status.Type == StatusType.NoteOn;

    public bool IsNoteOff => Status.Type == StatusType.NoteOff ||
                             (Status.Type == StatusType.NoteOn && DataB2OrLsb == 0);
}