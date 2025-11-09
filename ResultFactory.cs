using System.Runtime.CompilerServices;

namespace Midi.Net;

public static class ResultFactory
{
    public static Result From(bool success, string messageIfFailed, string? messageIfSuccess = null)
    {
        return success ? Success(messageIfSuccess) : Fail(messageIfFailed);
    }

    public static Result<T> From<T>(T? result, string messageIfFailed, string? messageIfSuccess = null)
    {
        return result != null
            ? Success(result, messageIfSuccess)
            : Fail<T>(messageIfFailed);
    }

    public static Result Success(string? message = null) => new(true, message);
    public static Result Fail(string message) => new(false, message);
    public static Result<T> Success<T>(T result, string? message = null) => new(result, true, message);
    public static Result<T> Fail<T>(string message) => new(default, false, message);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<TTo> Cast<TFrom, TTo>(this Result<TFrom> result) 
        where TFrom : class, TTo
        where TTo : class
    {
        return new Result<TTo>(result.Value, result.Success, result.Message);
    }

    public static async Task<Result<TTo>> Cast<TFrom, TTo>(this Task<Result<TFrom>> task) 
        where TFrom : class, TTo
        where TTo : class
    {
        return Cast<TFrom, TTo>(await task);
    }
    
    public static async ValueTask<Result<TTo>> Cast<TFrom, TTo>(this ValueTask<Result<TFrom>> task) 
        where TFrom : class, TTo
        where TTo : class
    {
        return Cast<TFrom, TTo>(await task);
    }
}