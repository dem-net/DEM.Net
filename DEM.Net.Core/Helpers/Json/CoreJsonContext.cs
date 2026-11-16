using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using DEM.Net.Core.EarthData;
using DEM.Net.Core.Stac;
using DEM.Net.Core.TileIndex;

namespace DEM.Net.Core;

/// <summary>
/// Source generated JSON contracts of the files the library reads and writes (catalog manifests, dataset indexes)
/// and of the remote payloads it parses (STAC, NASA CMR).
/// </summary>
/// <remarks>
/// Files written by former versions (Newtonsoft.Json) are read as is: property names are matched case insensitively,
/// numbers may be written as strings and NaN/Infinity as "NaN"/"Infinity".
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    Converters = new[] { typeof(UniversalDateTimeOffsetConverter) })]
[JsonSerializable(typeof(FileMetadata))]
[JsonSerializable(typeof(List<StacDemFile>))]
[JsonSerializable(typeof(List<NasaDemFile>))]
[JsonSerializable(typeof(List<TileIndexFileService.TileIndexEntry>))]
[JsonSerializable(typeof(CollectionItems))]
[JsonSerializable(typeof(NasaCmrGranuleResult))]
[JsonSerializable(typeof(Stac.Link), TypeInfoPropertyName = "StacLink")]
[JsonSerializable(typeof(List<Stac.Link>), TypeInfoPropertyName = "ListStacLink")]
[JsonSerializable(typeof(EarthData.Link), TypeInfoPropertyName = "EarthDataLink")]
[JsonSerializable(typeof(List<EarthData.Link>), TypeInfoPropertyName = "ListEarthDataLink")]
internal partial class CoreJsonContext : JsonSerializerContext
{
    private static CoreJsonContext? _indented;

    /// <summary>Same contracts, indented output (files meant to be read by humans).</summary>
    public static CoreJsonContext Indented => _indented ??= new CoreJsonContext(new JsonSerializerOptions(Default.Options) { WriteIndented = true });
}

/// <summary>Reads dates without offset as UTC (the former Newtonsoft settings used DateTimeStyles.AssumeUniversal).</summary>
internal sealed class UniversalDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTimeOffset.Parse(reader.GetString() ?? throw new JsonException("A date is expected."), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}

/// <summary>Maps JSON strings to enum values, several strings may map to the same value. Unknown strings are rejected.</summary>
internal abstract class MappedEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    private readonly (string Name, T Value)[] _map;

    protected MappedEnumConverter(params (string Name, T Value)[] map) => _map = map;

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        foreach (var (name, enumValue) in _map)
        {
            if (name == value)
                return enumValue;
        }
        throw new JsonException($"Cannot unmarshal type {typeof(T).Name}: {value ?? reader.TokenType.ToString()}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        foreach (var (name, enumValue) in _map)
        {
            if (EqualityComparer<T>.Default.Equals(enumValue, value))
            {
                writer.WriteStringValue(name);
                return;
            }
        }
        throw new JsonException($"Cannot marshal type {typeof(T).Name}: {value}");
    }
}
