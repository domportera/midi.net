using Midi.Net.MidiUtilityStructs.Enums;

namespace Midi.Net.MidiUtilityStructs;

public readonly record struct MidiNoteMessage(NoteId NoteId, byte Velocity);