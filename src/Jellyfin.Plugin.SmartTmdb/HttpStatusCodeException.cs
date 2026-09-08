using System.Net;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Exception for non-success HTTP status codes.
/// </summary>
public sealed class HttpStatusCodeException : System.Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpStatusCodeException"/> class.
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="message">Error message.</param>
    public HttpStatusCodeException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; }
}