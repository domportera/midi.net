namespace Midi.Net;

public interface IMidiDevice
{
    public bool IsConnected => MidiDevice is { ConnectionState: ConnectionState.Open };
    public MidiDevice MidiDevice { get; init; }
    Task<(bool Success, string? Error)> OnConnect();
    Task<(bool Success, string? Error)> CloseAsync();
}