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

public readonly record struct DeviceOpenResult<T>(T? Device, DeviceOpenResult Details, string? InitializationWarning = null)
{
    public bool IsSuccess => Details is {IsSuccess: true} && Device is not null;
    public static implicit operator bool (in DeviceOpenResult<T> result) => result.IsSuccess;
}

public readonly record struct DeviceOpenResult(PortOpenResult<IMidiInput> Input, PortOpenResult<IMidiOutput> Output)
{
    // convert to bool
    public bool IsSuccess => Input.IsSuccess && Output.IsSuccess;
}