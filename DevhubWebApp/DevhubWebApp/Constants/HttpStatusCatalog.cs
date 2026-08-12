using DevhubWebApp.Models.Tools.Http;

namespace DevhubWebApp.Constants;

/// <summary>
/// The HTTP status catalogue, held as data so the explorer renders a plain list and adding a code
/// is a one-line change here.
/// </summary>
public static class HttpStatusCatalog
{
    private static readonly IReadOnlyList<HttpStatusEntry> Catalogue =
    [
        // ---- 1xx ----
        Entry(100, "Continue", "The client may send the request body; used with the Expect header.",
            ["Uploading a large body after the server agrees to receive it"], [417]),
        Entry(101, "Switching Protocols", "The server is switching to the protocol the client asked for.",
            ["Upgrading an HTTP connection to WebSocket"], [426], ["Upgrade", "Connection"]),
        Entry(103, "Early Hints", "Preliminary headers sent before the final response, so the client can preload.",
            ["Sending Link headers for preloading while the page is generated"], [200], ["Link"]),

        // ---- 2xx ----
        Entry(200, "OK", "The request succeeded and the response carries the result.",
            ["A successful GET", "A PUT or POST that returns the updated resource"], [201, 204]),
        Entry(201, "Created", "The request succeeded and created a resource; Location points at it.",
            ["A POST that creates a record", "A PUT that creates a resource at a known URL"], [200, 202], ["Location"]),
        Entry(202, "Accepted", "The request was accepted but processing has not finished.",
            ["Queueing a long-running job", "Asynchronous imports"], [201, 303]),
        Entry(204, "No Content", "The request succeeded and there is deliberately no body.",
            ["A DELETE that succeeded", "A PUT whose result the client already has"], [200, 205]),
        Entry(205, "Reset Content", "The request succeeded and the client should reset its form view.",
            ["Clearing a form after a successful submission"], [204]),
        Entry(206, "Partial Content", "The response carries only the requested byte range.",
            ["Resumable downloads", "Video seeking"], [200, 416], ["Range", "Content-Range"]),

        // ---- 3xx ----
        Entry(301, "Moved Permanently", "The resource has a new permanent URL; clients should update links.",
            ["Retiring an old URL structure", "Redirecting HTTP to HTTPS"], [302, 308], ["Location"]),
        Entry(302, "Found", "The resource is temporarily at another URL. Method may change to GET.",
            ["Post-login redirects"], [301, 307], ["Location"]),
        Entry(303, "See Other", "Fetch the result of this request from another URL with GET.",
            ["The POST/redirect/GET pattern", "Pointing at the result of an accepted job"], [302, 307], ["Location"]),
        Entry(304, "Not Modified", "The cached copy is still valid, so no body is sent.",
            ["Conditional GET with If-None-Match or If-Modified-Since"], [200], ["ETag", "Last-Modified"]),
        Entry(307, "Temporary Redirect", "Like 302, but the method and body must be preserved.",
            ["Temporarily moving an endpoint that receives POSTs"], [302, 308], ["Location"]),
        Entry(308, "Permanent Redirect", "Like 301, but the method and body must be preserved.",
            ["Permanently moving a POST endpoint"], [301, 307], ["Location"]),

        // ---- 4xx ----
        Entry(400, "Bad Request", "The request is malformed and the server will not process it.",
            ["Invalid JSON", "A missing required field", "A parameter that fails validation"], [422, 404]),
        Entry(401, "Unauthorized", "Authentication is missing or invalid. The name is a misnomer: it means unauthenticated.",
            ["No credentials were sent", "An expired token"], [403], ["WWW-Authenticate"]),
        Entry(403, "Forbidden", "The caller is authenticated but not allowed to do this.",
            ["A user without the required role", "A tenant reaching another tenant's data"], [401, 404]),
        Entry(404, "Not Found", "The resource does not exist, or the server will not admit that it does.",
            ["An unknown id", "Hiding a resource the caller may not know about"], [410, 403]),
        Entry(405, "Method Not Allowed", "The URL exists but not for this HTTP method.",
            ["POSTing to a read-only endpoint"], [404, 501], ["Allow"]),
        Entry(406, "Not Acceptable", "No representation matches the client's Accept header.",
            ["A client asking for XML from a JSON-only API"], [415], ["Accept"]),
        Entry(408, "Request Timeout", "The client did not produce a request within the time the server was prepared to wait.",
            ["An idle connection the server closes", "A slow upload that stalls"], [504, 429]),
        Entry(409, "Conflict", "The request conflicts with the current state of the resource.",
            ["A duplicate unique key", "An edit against a stale version"], [412, 422]),
        Entry(410, "Gone", "The resource existed and has been removed on purpose.",
            ["A deleted account whose URL should not be retried"], [404]),
        Entry(412, "Precondition Failed", "A conditional header did not hold.",
            ["Optimistic concurrency with If-Match"], [409, 428], ["If-Match", "ETag"]),
        Entry(413, "Content Too Large", "The request body exceeds what the server accepts.",
            ["An upload beyond the size limit"], [400, 507]),
        Entry(415, "Unsupported Media Type", "The body's Content-Type is not one the endpoint accepts.",
            ["Sending form data to a JSON-only endpoint"], [406, 400], ["Content-Type"]),
        Entry(416, "Range Not Satisfiable", "The requested byte range lies outside the resource.",
            ["A resumed download whose file changed size"], [206], ["Content-Range"]),
        Entry(417, "Expectation Failed", "The server cannot meet the requirement in the Expect header.",
            ["A proxy that does not support Expect: 100-continue"], [100]),
        Entry(422, "Unprocessable Content", "The syntax is valid but the content fails business rules.",
            ["Validation failures on a well-formed payload"], [400, 409]),
        Entry(428, "Precondition Required", "The server requires the request to be conditional.",
            ["Forcing If-Match to prevent lost updates"], [412]),
        Entry(429, "Too Many Requests", "The client has been rate limited.",
            ["Throttling an API key", "Blunting a brute-force attempt"], [503], ["Retry-After", "RateLimit-Remaining"]),
        Entry(426, "Upgrade Required", "The client must switch to a different protocol to continue.",
            ["Requiring TLS", "Requiring a WebSocket upgrade"], [101], ["Upgrade"]),
        Entry(431, "Request Header Fields Too Large", "The headers are larger than the server accepts.",
            ["An oversized cookie or JWT in a header"], [413]),

        // ---- 5xx ----
        Entry(500, "Internal Server Error", "The server hit an unhandled condition. The catch-all failure.",
            ["An unhandled exception", "A bug in request handling"], [502, 503]),
        Entry(501, "Not Implemented", "The server does not support the functionality required.",
            ["An HTTP method the server never implements"], [405]),
        Entry(502, "Bad Gateway", "An upstream server returned an invalid response.",
            ["A proxy whose backend is broken or misconfigured"], [503, 504]),
        Entry(503, "Service Unavailable", "The server is temporarily unable to handle the request.",
            ["Planned maintenance", "Overload shedding"], [429, 502], ["Retry-After"]),
        Entry(504, "Gateway Timeout", "An upstream server did not respond in time.",
            ["A slow backend behind a proxy"], [502, 408]),
        Entry(507, "Insufficient Storage", "The server cannot store the representation needed.",
            ["A full disk on an upload endpoint"], [413])
    ];

    /// <summary>All known codes, ordered numerically.</summary>
    public static IReadOnlyList<HttpStatusEntry> All { get; } = Catalogue.OrderBy(entry => entry.Code).ToList();

    /// <summary>Classes present in the catalogue, in numeric order.</summary>
    public static IReadOnlyList<HttpStatusClass> Classes { get; } = Enum.GetValues<HttpStatusClass>();

    /// <summary>The class a numeric code belongs to, or <c>null</c> when it is outside 100-599.</summary>
    public static HttpStatusClass? ClassOf(int code) => code switch
    {
        >= 100 and < 200 => HttpStatusClass.Informational,
        >= 200 and < 300 => HttpStatusClass.Success,
        >= 300 and < 400 => HttpStatusClass.Redirection,
        >= 400 and < 500 => HttpStatusClass.ClientError,
        >= 500 and < 600 => HttpStatusClass.ServerError,
        _ => null
    };

    /// <summary>Display label of a class, e.g. <c>4xx client error</c>.</summary>
    public static string DescribeClass(HttpStatusClass statusClass) => statusClass switch
    {
        HttpStatusClass.Informational => "1xx informational",
        HttpStatusClass.Success => "2xx success",
        HttpStatusClass.Redirection => "3xx redirection",
        HttpStatusClass.ClientError => "4xx client error",
        _ => "5xx server error"
    };

    private static HttpStatusEntry Entry(int code, string name, string description,
        string[] useCases, int[] related, string[]? headers = null) => new()
        {
            Code = code,
            Name = name,
            Class = ClassOf(code) ?? HttpStatusClass.ServerError,
            Description = description,
            UseCases = useCases,
            Related = related,
            Headers = headers ?? []
        };
}
