using Application.Common.Errors;

namespace Application.Common;

public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value)
    {
        _value = value;
        IsSuccess = true;
        Error = Error.None;
    }

    private Result(Error error)
    {
        _value = default;
        IsSuccess = false;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    // Factory methods
    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(Error error) => Failure(error);

    // ── Monadic helpers ───────────────────────────────────────────────────

    /// <summary>Transforms the value if successful; propagates the error otherwise.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        IsSuccess ? Result<TOut>.Success(mapper(_value!)) : Result<TOut>.Failure(Error);

    /// <summary>Chains a subsequent Result-returning operation.</summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder) =>
        IsSuccess ? binder(_value!) : Result<TOut>.Failure(Error);

    /// <summary>Transforms the value asynchronously if successful; propagates the error otherwise.</summary>
    public async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper) =>
        IsSuccess ? Result<TOut>.Success(await mapper(_value!)) : Result<TOut>.Failure(Error);

    /// <summary>Chains a subsequent async Result-returning operation.</summary>
    public async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> binder) =>
        IsSuccess ? await binder(_value!) : Result<TOut>.Failure(Error);

    /// <summary>Executes one of two functions depending on success or failure.</summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(Error);
}

/// <summary>Non-generic Result for void operations (Create, Delete…).</summary>
public sealed class Result
{
    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);

    // ── Monadic helpers ───────────────────────────────────────────────────

    /// <summary>Chains a subsequent Result-returning operation.</summary>
    public Result Bind(Func<Result> binder) =>
        IsSuccess ? binder() : this;

    /// <summary>Chains a subsequent async Result-returning operation.</summary>
    public async Task<Result> BindAsync(Func<Task<Result>> binder) =>
        IsSuccess ? await binder() : this;

    /// <summary>Executes one of two functions depending on success or failure.</summary>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error);
}
