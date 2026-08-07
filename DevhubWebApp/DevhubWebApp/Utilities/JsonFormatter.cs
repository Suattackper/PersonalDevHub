using System.Buffers;
using System.Text;
using System.Text.Json;
using DevhubWebApp.Models.Tools.Json;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Pure JSON transformations. No dependencies, no UI, no I/O — which is what makes the tool
/// testable and keeps the component free of logic.
/// <para>
/// Documents are re-written through <see cref="Utf8JsonWriter"/> rather than string-patched, so
/// escaping, numbers and Unicode survive a round-trip untouched.
/// </para>
/// </summary>
public static class JsonFormatter
{
    /// <summary>
    /// Relaxed escaping: quotes stay as <c>\"</c> and HTML-sensitive characters are not escaped
    /// defensively, so the output reads the way the user wrote it. Characters outside the BMP
    /// (emoji) are still emitted as <c>\uXXXX</c> surrogate pairs — that is a limitation of every
    /// built-in .NET encoder, and it is lossless: parsing the output yields the same value.
    /// </summary>
    private static readonly System.Text.Encodings.Web.JavaScriptEncoder Encoder =
        System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

    private static readonly JsonDocumentOptions ParseOptions = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 256
    };

    /// <summary>Re-writes the document with indentation.</summary>
    public static JsonOperationResult Pretty(string? json, JsonFormatOptions? options = null)
        => Rewrite(json, indented: true, options ?? JsonFormatOptions.Default);

    /// <summary>Re-writes the document without any insignificant whitespace.</summary>
    public static JsonOperationResult Minify(string? json, JsonFormatOptions? options = null)
        => Rewrite(json, indented: false, options ?? JsonFormatOptions.Default);

    /// <summary>Parses without producing output. Returns <c>null</c> when the document is valid.</summary>
    public static JsonError? Validate(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(json, ParseOptions);
            return null;
        }
        catch (JsonException exception)
        {
            return ToError(exception);
        }
    }

    /// <summary>
    /// Validates, counts duplicates and optionally builds the display tree in a <em>single</em>
    /// parse. Editors call this on every debounced keystroke, so parsing a multi-megabyte document
    /// three times over would be felt immediately.
    /// </summary>
    public static JsonInspection Inspect(string? json, bool buildTree = true)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return JsonInspection.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(json, ParseOptions);

            return new JsonInspection
            {
                DuplicateCount = CountDuplicates(document.RootElement),
                Tree = buildTree ? JsonTreeBuilder.Build(document.RootElement) : null
            };
        }
        catch (JsonException exception)
        {
            return new JsonInspection { Error = ToError(exception) };
        }
    }

    /// <summary>
    /// Counts properties that are shadowed by a later property of the same name, at any depth.
    /// Reported to the user before offering to remove them.
    /// </summary>
    public static int CountDuplicateProperties(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return 0;
        }

        try
        {
            using var document = JsonDocument.Parse(json, ParseOptions);
            return CountDuplicates(document.RootElement);
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static JsonOperationResult Rewrite(string? json, bool indented, JsonFormatOptions options)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return JsonOperationResult.Success(string.Empty);
        }

        try
        {
            using var document = JsonDocument.Parse(json, ParseOptions);
            // Pre-sizing avoids the repeated grow-and-copy cycle a megabyte-scale rewrite would
            // otherwise pay for; indentation roughly doubles the payload in the worst case.
            var buffer = new ArrayBufferWriter<byte>(Math.Max(json.Length * (indented ? 2 : 1), 1024));
            var removed = 0;

            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
            {
                Indented = indented,
                Encoder = Encoder,
                SkipValidation = false
            }))
            {
                if (options.SortProperties || options.RemoveDuplicateProperties)
                {
                    WriteElement(document.RootElement, writer, options, ref removed);
                }
                else
                {
                    // Fast path: with no reordering to do, the runtime copies the parsed document
                    // in one pass instead of us walking it node by node. On multi-megabyte inputs
                    // this is the difference between a moment and several seconds.
                    document.RootElement.WriteTo(writer);
                }
            }

            return JsonOperationResult.Success(Encoding.UTF8.GetString(buffer.WrittenSpan), removed);
        }
        catch (JsonException exception)
        {
            return JsonOperationResult.Failure(ToError(exception));
        }
    }

    private static void WriteElement(JsonElement element, Utf8JsonWriter writer, JsonFormatOptions options, ref int removed)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in GetProperties(element, options, ref removed))
                {
                    writer.WritePropertyName(property.Name);
                    WriteElement(property.Value, writer, options, ref removed);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteElement(item, writer, options, ref removed);
                }

                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    /// <summary>
    /// Applies the duplicate and ordering rules to one object's properties. Deduplication keeps the
    /// last occurrence, matching how JSON parsers resolve repeated keys.
    /// </summary>
    private static IEnumerable<JsonProperty> GetProperties(JsonElement element, JsonFormatOptions options, ref int removed)
    {
        if (!options.RemoveDuplicateProperties && !options.SortProperties)
        {
            return element.EnumerateObject();
        }

        var properties = element.EnumerateObject().ToList();

        if (options.RemoveDuplicateProperties)
        {
            var deduplicated = new Dictionary<string, JsonProperty>(StringComparer.Ordinal);
            var order = new List<string>();

            foreach (var property in properties)
            {
                if (!deduplicated.TryAdd(property.Name, property))
                {
                    deduplicated[property.Name] = property;
                    removed++;
                }
                else
                {
                    order.Add(property.Name);
                }
            }

            properties = order.Select(name => deduplicated[name]).ToList();
        }

        return options.SortProperties
            ? properties.OrderBy(property => property.Name, StringComparer.Ordinal)
            : properties;
    }

    private static int CountDuplicates(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var count = 0;

                foreach (var property in element.EnumerateObject())
                {
                    if (!seen.Add(property.Name))
                    {
                        count++;
                    }

                    count += CountDuplicates(property.Value);
                }

                return count;

            case JsonValueKind.Array:
                return element.EnumerateArray().Sum(CountDuplicates);

            default:
                return 0;
        }
    }

    /// <summary>
    /// Converts parser coordinates to 1-based line and column. <see cref="JsonException"/> reports a
    /// 0-based line and a byte offset within it, and its message repeats those numbers, so both are
    /// normalised here and the trailing position clause is stripped.
    /// </summary>
    private static JsonError ToError(JsonException exception)
    {
        var message = exception.Message;
        var separator = message.IndexOf(" LineNumber:", StringComparison.Ordinal);

        return new JsonError
        {
            Message = separator > 0 ? message[..separator].Trim() : message,
            Line = (int)(exception.LineNumber ?? 0) + 1,
            Column = (int)(exception.BytePositionInLine ?? 0) + 1
        };
    }
}
