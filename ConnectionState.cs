using Commons.Music.Midi;

namespace Midi.Net;

public enum ConnectionState
{
    Closed = MidiPortConnectionState.Closed,
    Open = MidiPortConnectionState.Open,
    Pending = MidiPortConnectionState.Pending,
}