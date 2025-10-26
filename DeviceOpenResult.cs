namespace Midi.Net;

public enum DeviceOpenResult
{
    Success,
    InputNotFound,
    OutputNotFound,
    InputOpenFailed,
    OutputOpenFailed
}


public readonly record struct DeviceOpenResult<T>(DeviceOpenResult Info, T? Device);

public readonly record struct DeviceOpenResult<TInput, TOutput>(DeviceOpenResult Info, TInput? Input, TOutput? Output);

public static class DeviceOpenResultExtensions
{
    // todo - extension properties / extension operators when .NET 10 stable is released
    public static bool IsSuccess(this DeviceOpenResult result) => result == DeviceOpenResult.Success;
    public static bool IsSuccess<T>(this DeviceOpenResult<T> result) => result.Info == DeviceOpenResult.Success;
    public static bool IsSuccess<TInput, TOutput>(this DeviceOpenResult<TInput, TOutput> result) =>
        result.Info == DeviceOpenResult.Success;
}