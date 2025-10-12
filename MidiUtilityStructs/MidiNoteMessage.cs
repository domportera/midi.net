namespace LinnstrumentKeyboard;

public readonly record struct MidiNoteMessage(bool IsNoteOff, NoteId NoteId, byte Velocity);