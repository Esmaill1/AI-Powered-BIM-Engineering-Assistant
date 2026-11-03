using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuleCheck.Core.Rules;

public enum RuleValueKind { Text, Number, Boolean, List }

/// <summary>
/// Value of a where-condition: a string, number, boolean, or a list of strings/numbers (for <c>in</c>).
/// </summary>
[JsonConverter(typeof(RuleValueJsonConverter))]
public sealed record RuleValue
{
    public RuleValueKind Kind { get; private init; }
    public string? Text { get; private init; }
    public double? Number { get; private init; }
    public bool? Boolean { get; private init; }
    public IReadOnlyList<RuleValue>? Items { get; private init; }

    public static RuleValue Of(string value) => new() { Kind = RuleValueKind.Text, Text = value };
    public static RuleValue Of(double value) => new() { Kind = RuleValueKind.Number, Number = value };
    public static RuleValue Of(bool value) => new() { Kind = RuleValueKind.Boolean, Boolean = value };

    public static RuleValue ListOf(params RuleValue[] items)
    {
        if (items.Any(i => i.Kind is RuleValueKind.List or RuleValueKind.Boolean))
            throw new ArgumentException("List items must be strings or numbers.", nameof(items));
        return new() { Kind = RuleValueKind.List, Items = items };
    }

    public static RuleValue ListOf(params string[] items) => ListOf(items.Select(Of).ToArray());

    public override string ToString() => Kind switch
    {
        RuleValueKind.Text => Text!,
        RuleValueKind.Number => Number!.Value.ToString("R", CultureInfo.InvariantCulture),
        RuleValueKind.Boolean => Boolean!.Value ? "true" : "false",
        _ => "[" + string.Join(", ", Items!.Select(i => i.ToString())) + "]",
    };
}

public sealed class RuleValueJsonConverter : JsonConverter<RuleValue>
{
    public override RuleValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartArray:
                var items = new List<RuleValue>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    items.Add(ReadScalar(ref reader, allowBoolean: false));
                return RuleValue.ListOf(items.ToArray());
            default:
                return ReadScalar(ref reader, allowBoolean: true);
        }
    }

    private static RuleValue ReadScalar(ref Utf8JsonReader reader, bool allowBoolean) => reader.TokenType switch
    {
        JsonTokenType.String => RuleValue.Of(reader.GetString()!),
        JsonTokenType.Number => RuleValue.Of(reader.GetDouble()),
        JsonTokenType.True when allowBoolean => RuleValue.Of(true),
        JsonTokenType.False when allowBoolean => RuleValue.Of(false),
        _ => throw new JsonException($"Unsupported rule value token: {reader.TokenType}."),
    };

    public override void Write(Utf8JsonWriter writer, RuleValue value, JsonSerializerOptions options)
    {
        switch (value.Kind)
        {
            case RuleValueKind.Text: writer.WriteStringValue(value.Text); break;
            case RuleValueKind.Number: writer.WriteNumberValue(value.Number!.Value); break;
            case RuleValueKind.Boolean: writer.WriteBooleanValue(value.Boolean!.Value); break;
            default:
                writer.WriteStartArray();
                foreach (var item in value.Items!) Write(writer, item, options);
                writer.WriteEndArray();
                break;
        }
    }
}
