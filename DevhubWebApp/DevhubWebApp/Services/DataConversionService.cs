using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools.Data;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace DevhubWebApp.Services;

/// <summary>
/// Converts between JSON, YAML and XML.
/// <para>
/// Every conversion goes through a common in-memory shape (<see cref="JsonNode"/>): each format is
/// parsed into it and written out from it. That is what keeps six directions to three readers and
/// three writers instead of six bespoke translators.
/// </para>
/// </summary>
public sealed class DataConversionService : IDataConversionService
{
    /// <summary>Element name used when XML needs a root that the source did not provide.</summary>
    private const string DefaultRootName = "root";

    /// <summary>Attribute name prefix used when round-tripping XML attributes through JSON.</summary>
    private const string AttributePrefix = "@";

    /// <summary>Property holding an element's text when it also carries attributes.</summary>
    private const string TextProperty = "#text";

    private static readonly JsonSerializerOptions IndentedJson = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions CompactJson = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public DataResult Convert(string? input, DataFormat from, DataFormat to, bool indented = true)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DataResult.Empty;
        }

        try
        {
            var node = Read(input, from);
            return DataResult.Success(Write(node, to, indented));
        }
        catch (Exception exception) when (IsParseFailure(exception))
        {
            return Describe(exception, from);
        }
    }

    public DataResult Format(string? input, DataFormat format, bool indented)
        => Convert(input, format, format, indented);

    public DataResult Validate(string? input, DataFormat format)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DataResult.Empty;
        }

        try
        {
            Read(input, format);
            return DataResult.Success(string.Empty);
        }
        catch (Exception exception) when (IsParseFailure(exception))
        {
            return Describe(exception, format);
        }
    }

    /// <summary>
    /// Sniffs the format from the first meaningful character: <c>&lt;</c> is XML, <c>{</c> or
    /// <c>[</c> is JSON, anything else is treated as YAML — which is also a superset of JSON, so
    /// the guess is safe when it is wrong in that direction.
    /// </summary>
    public DataFormat Detect(string? input, DataFormat fallback = DataFormat.Json)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return fallback;
        }

        foreach (var character in input)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            return character switch
            {
                '<' => DataFormat.Xml,
                '{' or '[' => DataFormat.Json,
                _ => DataFormat.Yaml
            };
        }

        return fallback;
    }

    public string GetExtension(DataFormat format) => format switch
    {
        DataFormat.Yaml => "yaml",
        DataFormat.Xml => "xml",
        _ => "json"
    };

    public string GetMimeType(DataFormat format) => format switch
    {
        DataFormat.Yaml => "application/yaml",
        DataFormat.Xml => MimeTypes.Xml,
        _ => MimeTypes.Json
    };

    // ---------- readers ----------

    private static JsonNode? Read(string input, DataFormat format) => format switch
    {
        DataFormat.Yaml => ReadYaml(input),
        DataFormat.Xml => ReadXml(input),
        _ => JsonNode.Parse(input)
    };

    private static JsonNode? ReadYaml(string input)
    {
        var deserializer = new DeserializerBuilder().Build();
        var graph = deserializer.Deserialize<object?>(new StringReader(input));

        return ToNode(graph);
    }

    /// <summary>Maps YamlDotNet's loosely-typed graph onto JSON nodes.</summary>
    private static JsonNode? ToNode(object? value)
    {
        switch (value)
        {
            case null:
                return null;

            case IDictionary<object, object> map:
                var jsonObject = new JsonObject();
                foreach (var pair in map)
                {
                    jsonObject[pair.Key?.ToString() ?? string.Empty] = ToNode(pair.Value);
                }

                return jsonObject;

            case IList<object> list:
                var jsonArray = new JsonArray();
                foreach (var item in list)
                {
                    jsonArray.Add(ToNode(item));
                }

                return jsonArray;

            default:
                return ToScalar(value.ToString());
        }
    }

    /// <summary>
    /// YAML scalars arrive as strings. Numbers and booleans are recovered so a JSON export is
    /// typed rather than quoted; anything else stays a string.
    /// </summary>
    private static JsonValue? ToScalar(string? text)
    {
        if (text is null)
        {
            return null;
        }

        if (bool.TryParse(text, out var boolean))
        {
            return JsonValue.Create(boolean);
        }

        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
        {
            return JsonValue.Create(integer);
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return JsonValue.Create(number);
        }

        return JsonValue.Create(text);
    }

    private static JsonObject ReadXml(string input)
    {
        var document = XDocument.Parse(input, LoadOptions.None);
        var root = document.Root ?? throw new XmlException("The document has no root element.");

        return new JsonObject { [root.Name.LocalName] = ElementToNode(root) };
    }

    private static JsonNode? ElementToNode(XElement element)
    {
        var hasChildren = element.HasElements;
        var hasAttributes = element.HasAttributes;

        if (!hasChildren && !hasAttributes)
        {
            return ToScalar(element.Value);
        }

        var node = new JsonObject();

        foreach (var attribute in element.Attributes())
        {
            node[$"{AttributePrefix}{attribute.Name.LocalName}"] = ToScalar(attribute.Value);
        }

        // Repeated child names become an array, which is what a reader expects.
        foreach (var group in element.Elements().GroupBy(child => child.Name.LocalName))
        {
            if (group.Count() == 1)
            {
                node[group.Key] = ElementToNode(group.First());
                continue;
            }

            var array = new JsonArray();
            foreach (var child in group)
            {
                array.Add(ElementToNode(child));
            }

            node[group.Key] = array;
        }

        if (!hasChildren && hasAttributes && !string.IsNullOrWhiteSpace(element.Value))
        {
            node[TextProperty] = ToScalar(element.Value);
        }

        return node;
    }

    // ---------- writers ----------

    private static string Write(JsonNode? node, DataFormat format, bool indented) => format switch
    {
        DataFormat.Yaml => WriteYaml(node),
        DataFormat.Xml => WriteXml(node, indented),
        _ => node?.ToJsonString(indented ? IndentedJson : CompactJson) ?? "null"
    };

    private static string WriteYaml(JsonNode? node)
    {
        var serializer = new SerializerBuilder().Build();
        return serializer.Serialize(FromNode(node)).TrimEnd();
    }

    /// <summary>Maps JSON nodes back onto plain objects that YamlDotNet knows how to write.</summary>
    private static object? FromNode(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return null;

            case JsonObject jsonObject:
                return jsonObject.ToDictionary(pair => pair.Key, pair => FromNode(pair.Value));

            case JsonArray jsonArray:
                return jsonArray.Select(FromNode).ToList();

            case JsonValue value:
                return value.GetValueKind() switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,

                    // A JsonValue holds its original CLR type, so an integer must be asked for as
                    // one; requesting a double from a long-backed value throws.
                    JsonValueKind.Number => value.TryGetValue<long>(out var integer)
                        ? integer
                        : value.TryGetValue<double>(out var number) ? number : value.ToString(),

                    _ => value.ToString()
                };

            default:
                return node.ToString();
        }
    }

    private static string WriteXml(JsonNode? node, bool indented)
    {
        var root = BuildXmlRoot(node);
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null), root);

        var settings = new XmlWriterSettings
        {
            Indent = indented,
            IndentChars = "  ",
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(false)
        };

        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, settings))
        {
            document.Save(writer);
        }

        return builder.ToString();
    }

    /// <summary>
    /// XML needs exactly one root. A single-property object supplies it; anything else is wrapped,
    /// because an array or a scalar has no name of its own.
    /// </summary>
    private static XElement BuildXmlRoot(JsonNode? node)
    {
        if (node is JsonObject { Count: 1 } single)
        {
            var pair = single.First();
            return BuildElement(SafeName(pair.Key), pair.Value);
        }

        return BuildElement(DefaultRootName, node);
    }

    private static XElement BuildElement(string name, JsonNode? node)
    {
        var element = new XElement(name);

        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var pair in jsonObject)
                {
                    if (pair.Key.StartsWith(AttributePrefix, StringComparison.Ordinal))
                    {
                        element.SetAttributeValue(SafeName(pair.Key[1..]), pair.Value?.ToString() ?? string.Empty);
                        continue;
                    }

                    if (pair.Key == TextProperty)
                    {
                        element.Add(new XText(pair.Value?.ToString() ?? string.Empty));
                        continue;
                    }

                    AddChild(element, SafeName(pair.Key), pair.Value);
                }

                break;

            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    element.Add(BuildElement("item", item));
                }

                break;

            case null:
                break;

            default:
                element.Add(new XText(node.ToString()));
                break;
        }

        return element;
    }

    private static void AddChild(XElement parent, string name, JsonNode? value)
    {
        // An array under a name becomes repeated elements of that name, mirroring the reader.
        if (value is JsonArray array)
        {
            foreach (var item in array)
            {
                parent.Add(BuildElement(name, item));
            }

            return;
        }

        parent.Add(BuildElement(name, value));
    }

    /// <summary>
    /// Turns an arbitrary key into a legal XML name. JSON allows keys that XML forbids, and a
    /// silent exception here would be a confusing way to learn that.
    /// </summary>
    private static string SafeName(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "item";
        }

        var builder = new StringBuilder(key.Length);

        foreach (var character in key)
        {
            var isValid = char.IsLetterOrDigit(character) || character is '_' or '-' or '.';
            builder.Append(isValid ? character : '_');
        }

        var name = builder.ToString();
        return char.IsLetter(name[0]) || name[0] == '_' ? name : $"_{name}";
    }

    // ---------- diagnostics ----------

    private static bool IsParseFailure(Exception exception)
        => exception is JsonException or XmlException or YamlException or FormatException or InvalidCastException;

    /// <summary>Turns a parser exception into a message and, where available, a line number.</summary>
    private static DataResult Describe(Exception exception, DataFormat format) => exception switch
    {
        JsonException json => DataResult.Failure(Trim(json.Message), json.LineNumber is null ? null : (int)json.LineNumber + 1),
        XmlException xml => DataResult.Failure(Trim(xml.Message), xml.LineNumber),
        YamlException yaml => DataResult.Failure(Trim(yaml.Message), (int)yaml.Start.Line),
        _ => DataResult.Failure($"This does not parse as {format.ToString().ToUpperInvariant()}: {Trim(exception.Message)}")
    };

    private static string Trim(string message)
    {
        var separator = message.IndexOf(" LineNumber:", StringComparison.Ordinal);
        return separator > 0 ? message[..separator].Trim() : message;
    }
}

