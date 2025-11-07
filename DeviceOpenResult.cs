using Commons.Music.Midi;

namespace Midi.Net;

public enum PortOpenStatus
{
    Success,
    NotFound,
    OpenPending,
    OpenFailed,
    OpenException,
}

public readonly record struct PortOpenResult<T>(PortOpenStatus Status, T? Port, IMidiPortDetails? Details) where T : IMidiPort
{
    // convert to bool
    public bool IsSuccess => Status == PortOpenStatus.Success;
    public static implicit operator bool (in PortOpenResult<T> result) => result.Status == PortOpenStatus.Success;
    
}

public readonly record struct DeviceOpenResult(MidiDevice? MidiDevice, PortOpenResult<IMidiInput> Input, PortOpenResult<IMidiOutput> Output)
{
    // convert to bool
    public bool IsSuccess => Input.IsSuccess && Output.IsSuccess && MidiDevice is {ConnectionState: ConnectionState.Open};
}