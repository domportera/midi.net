using Midi.Net.MidiUtilityStructs.Enums;

namespace Midi.Net.MidiUtilityStructs;

public readonly record struct MidiNoteMessage(bool IsNoteOff, NoteId NoteId, byte Velocity);