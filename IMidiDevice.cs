namespace Midi.Net;

public interface IMidiDevice
{
    public MidiDevice MidiDevice { get; init; }
    Task OnConnect();
    Task<(bool Success, string? Error)> OnClose();
}