using System.Text.Json;
using DevhubWebApp.Models.Tools.Data;
using DevhubWebApp.Services;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Conversion, formatting and validation rules across JSON, YAML and XML.</summary>
public class DataConversionServiceTests
{
    private readonly DataConversionService _service = new();

    private const string Json = """
        {
          "name": "DevHub",
          "offline": true,
          "tools": 25,
          "tags": ["json", "yaml"],
          "owner": { "team": "platform" }
        }
        """;

    [Fact]
    public void JsonToYaml_ProducesReadableYaml()
    {
        var result = _service.Convert(Json, DataFormat.Json, DataFormat.Yaml);

        Assert.True(result.IsSuccess);
        Assert.Contains("name: DevHub", result.Output, StringComparison.Ordinal);
        Assert.Contains("tools: 25", result.Output, StringComparison.Ordinal);
        Assert.Contains("- json", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void YamlToJson_RecoversTypesRatherThanQuotingEverything()
    {
        const string yaml = """
            name: DevHub
            offline: true
            tools: 25
            ratio: 1.5
            """;

        var result = _service.Convert(yaml, DataFormat.Yaml, DataFormat.Json);

        Assert.True(result.IsSuccess);

        using var document = JsonDocument.Parse(result.Output);
        Assert.Equal("DevHub", document.RootElement.GetProperty("name").GetString());
        Assert.True(document.RootElement.GetProperty("offline").GetBoolean());
        Assert.Equal(25, document.RootElement.GetProperty("tools").GetInt32());
        Assert.Equal(1.5, document.RootElement.GetProperty("ratio").GetDouble());
    }

    [Fact]
    public void JsonToYamlToJson_RoundTripsTheValues()
    {
        var yaml = _service.Convert(Json, DataFormat.Json, DataFormat.Yaml).Output;
        var json = _service.Convert(yaml, DataFormat.Yaml, DataFormat.Json).Output;

        using var document = JsonDocument.Parse(json);
        Assert.Equal("DevHub", document.RootElement.GetProperty("name").GetString());
        Assert.Equal(2, document.RootElement.GetProperty("tags").GetArrayLength());
        Assert.Equal("platform", document.RootElement.GetProperty("owner").GetProperty("team").GetString());
    }

    [Fact]
    public void JsonToXml_UsesASinglePropertyAsTheRoot()
    {
        var result = _service.Convert("""{"config":{"port":5199}}""", DataFormat.Json, DataFormat.Xml);

        Assert.True(result.IsSuccess);
        Assert.Contains("<config>", result.Output, StringComparison.Ordinal);
        Assert.Contains("<port>5199</port>", result.Output, StringComparison.Ordinal);
    }

    /// <summary>XML needs exactly one root, so a multi-property object has to be wrapped.</summary>
    [Fact]
    public void JsonToXml_WrapsWhenThereIsNoSingleRoot()
    {
        var result = _service.Convert("""{"a":1,"b":2}""", DataFormat.Json, DataFormat.Xml);

        Assert.True(result.IsSuccess);
        Assert.Contains("<root>", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonToXml_TurnsArraysIntoRepeatedElements()
    {
        var result = _service.Convert("""{"list":{"item":[1,2,3]}}""", DataFormat.Json, DataFormat.Xml);

        Assert.Equal(3, result.Output.Split("<item>").Length - 1);
    }

    [Fact]
    public void XmlToJson_MapsAttributesAndRepeatedElements()
    {
        const string xml = """
            <server name="api" port="443">
              <route>/health</route>
              <route>/metrics</route>
            </server>
            """;

        var result = _service.Convert(xml, DataFormat.Xml, DataFormat.Json);

        Assert.True(result.IsSuccess);

        using var document = JsonDocument.Parse(result.Output);
        var server = document.RootElement.GetProperty("server");

        Assert.Equal("api", server.GetProperty("@name").GetString());
        Assert.Equal(443, server.GetProperty("@port").GetInt32());
        Assert.Equal(2, server.GetProperty("route").GetArrayLength());
    }

    [Fact]
    public void XmlToJsonToXml_RoundTripsStructure()
    {
        const string xml = "<config><port>5199</port><debug>true</debug></config>";

        var json = _service.Convert(xml, DataFormat.Xml, DataFormat.Json).Output;
        var roundTripped = _service.Convert(json, DataFormat.Json, DataFormat.Xml).Output;

        Assert.Contains("<config>", roundTripped, StringComparison.Ordinal);
        Assert.Contains("<port>5199</port>", roundTripped, StringComparison.Ordinal);
    }

    [Fact]
    public void YamlToXml_AndXmlToYaml_Work()
    {
        const string yaml = "config:\n  port: 5199";

        var xml = _service.Convert(yaml, DataFormat.Yaml, DataFormat.Xml);
        Assert.True(xml.IsSuccess);
        Assert.Contains("<port>5199</port>", xml.Output, StringComparison.Ordinal);

        var backToYaml = _service.Convert(xml.Output, DataFormat.Xml, DataFormat.Yaml);
        Assert.True(backToYaml.IsSuccess);
        Assert.Contains("port: 5199", backToYaml.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_IndentsAndMinifiesJson()
    {
        var minified = _service.Format(Json, DataFormat.Json, indented: false);
        var indented = _service.Format(minified.Output, DataFormat.Json, indented: true);

        Assert.DoesNotContain("\n", minified.Output, StringComparison.Ordinal);
        Assert.Contains("\n", indented.Output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{ bad json", DataFormat.Json)]
    [InlineData("<unclosed>", DataFormat.Xml)]
    [InlineData("key: [unclosed", DataFormat.Yaml)]
    public void Validate_ReportsTheReasonForUnparsableInput(string input, DataFormat format)
    {
        var result = _service.Validate(input, format);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Validate_ReportsALineNumberWhereTheParserGivesOne()
    {
        var result = _service.Validate("{\n  \"a\": 1,\n  \"b\" 2\n}", DataFormat.Json);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorLine);
    }

    [Fact]
    public void Validate_AcceptsWellFormedInput()
    {
        Assert.True(_service.Validate(Json, DataFormat.Json).IsSuccess);
        Assert.True(_service.Validate("<a><b/></a>", DataFormat.Xml).IsSuccess);
        Assert.True(_service.Validate("a: 1", DataFormat.Yaml).IsSuccess);
    }

    [Fact]
    public void Convert_ReturnsEmptyForBlankInput()
        => Assert.Same(DataResult.Empty, _service.Convert("   ", DataFormat.Json, DataFormat.Yaml));

    [Fact]
    public void Convert_ReportsAFailureRatherThanThrowing()
    {
        var result = _service.Convert("{ nope", DataFormat.Json, DataFormat.Yaml);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Output);
    }

    [Theory]
    [InlineData("{\"a\":1}", DataFormat.Json)]
    [InlineData("[1,2]", DataFormat.Json)]
    [InlineData("  <root/>", DataFormat.Xml)]
    [InlineData("a: 1", DataFormat.Yaml)]
    public void Detect_GuessesTheFormatFromTheFirstMeaningfulCharacter(string input, DataFormat expected)
        => Assert.Equal(expected, _service.Detect(input));

    [Fact]
    public void Detect_FallsBackWhenThereIsNothingToGoOn()
        => Assert.Equal(DataFormat.Yaml, _service.Detect("   ", DataFormat.Yaml));

    [Fact]
    public void JsonToXml_SanitisesKeysThatAreNotLegalXmlNames()
    {
        var result = _service.Convert("""{"not a name":{"$weird":1}}""", DataFormat.Json, DataFormat.Xml);

        Assert.True(result.IsSuccess);
        Assert.Contains("<not_a_name>", result.Output, StringComparison.Ordinal);
        Assert.Contains("<_weird>1</_weird>", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtensionsAndMimeTypes_MatchEachFormat()
    {
        Assert.Equal("yaml", _service.GetExtension(DataFormat.Yaml));
        Assert.Equal("xml", _service.GetExtension(DataFormat.Xml));
        Assert.Equal("application/json", _service.GetMimeType(DataFormat.Json));
        Assert.Equal("application/yaml", _service.GetMimeType(DataFormat.Yaml));
    }
}
