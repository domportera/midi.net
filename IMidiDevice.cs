using Commons.Music.Midi;

namespace Midi.Net;

public interface IMidiDevice
{
    public bool IsConnected => MidiDevice is { ConnectionState: ConnectionState.Open };
    public MidiDevice MidiDevice { get; init; }
    Task<Result> CloseAsync() => MidiDevice.CloseAsync();

    void BeginConnect() => MidiDevice.BeginConnect();

    IMidiInput Input => MidiDevice.Input;
    IMidiOutput Output => MidiDevice.Output;
    public event AsyncEventHandler<bool> ConnectionStateChanged
    {
        add => MidiDevice.ConnectionStateChanged += value;
        remove => MidiDevice.ConnectionStateChanged -= value;
    }
}