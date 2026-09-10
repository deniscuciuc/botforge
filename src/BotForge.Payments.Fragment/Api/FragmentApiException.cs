using System.Net;

namespace BotForge.Payments.Fragment.Api;

internal sealed class FragmentApiException(
    string message,
    HttpStatusCode statusCode,
    IReadOnlyList<Error>? errors = null,
    Exception? inner = null
) : Exception(errors is { Count: > 0 }
    ? message + ": " + string.Join(", ", errors.Select(e => e.Code + ":" + e.Message))
    : message, inner)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public IReadOnlyList<Error>? Errors { get; } = errors;
}
