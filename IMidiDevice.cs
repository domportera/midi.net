using System.Diagnostics.CodeAnalysis;

namespace Midi.Net;

public interface IMidiDevice
{
    public bool IsConnected => MidiDevice is { ConnectionState: ConnectionState.Open };
    public MidiDevice MidiDevice { get; init; }
    Task<Result> OnConnect();
    Task<Result> CloseAsync();
    
}

public readonly record struct Result<T>
{
    public T? Value { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Message))]
    public bool Success { get; }
    
    public string? Message { get; }
    
    
    
    public Result(T? value, bool success, string? error)
    {
        Value = value;
        Success = success;
        Message = error;

        switch (success)
        {
            case true when value is null:
                throw new InvalidOperationException("Value required for successful result");
            case false when error is null:
                throw new InvalidOperationException("Error message required for failed result");
        }
    }
    
    public static implicit operator Result(Result<T> result) => new(result.Success, result.Message);
}

public readonly struct Result
{
    public Result(bool success, string? message)
    {
        Success = success;
        Message = message;

        if (!success && message is null)
        {
            throw new InvalidOperationException("Error message required for failed result");
        }
    }

    public bool Success { get; }
    public string? Message { get; }
    
    public static implicit operator bool(Result result) => result.Success;
}