namespace DanaCotech.Application.Common;

public sealed class ServiceResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    public static ServiceResponse<T> Ok(T data, string message = "Request completed successfully") =>
        new() { Success = true, Message = message, Data = data };

    public static ServiceResponse<T> Fail(string message, IReadOnlyCollection<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? Array.Empty<string>() };
}
