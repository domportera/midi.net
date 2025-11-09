using System.Diagnostics.CodeAnalysis;

namespace Midi.Net;

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

public readonly record struct Result<T>
{
    public T? Value { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Message))]
    public bool Success { get; }

    public string? Message { get; }
    

    internal Result(T? value, bool success, string? message)
    {
        Value = value;
        Success = success;
        Message = message;
    }

    public static implicit operator Result(Result<T> result) => new(result.Success, result.Message);
}