namespace CallSense.SharedKernel;

/// <summary>
/// Represents the outcome of an operation — either success with a value, or failure with an error.
/// Use this instead of throwing exceptions for expected business failures.
///
/// Usage:
///   return Result&lt;Guid&gt;.Success(callId);
///   return Result&lt;Guid&gt;.Failure("File type not supported");
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
