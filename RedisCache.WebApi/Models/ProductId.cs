using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedisCache.WebApi.Models;

[TypeConverter(typeof(ProductIdTypeConverter))]
[JsonConverter(typeof(ProductIdJsonConverter))]
public readonly record struct ProductId(Guid Value)
{
    public override string ToString() => Value.ToString();
    public static ProductId New() => new ProductId(Guid.NewGuid());
    public bool IsDefault => Value == Guid.Empty;
    public static bool TryParse(string? s, out ProductId id)
    {
        if (Guid.TryParse(s, out var g)) { id = new ProductId(g); return true; }
        id = default; return false;
    }
    public static ProductId Parse(string s) => new ProductId(Guid.Parse(s));

    public static implicit operator Guid(ProductId id) => id.Value;
    public static implicit operator ProductId(Guid value) => new ProductId(value);
}

public sealed class ProductIdTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s && Guid.TryParse(s, out var g)) return new ProductId(g);
        return base.ConvertFrom(context, culture, value);
    }
}

public sealed class ProductIdJsonConverter : JsonConverter<ProductId>
{
    public override ProductId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (Guid.TryParse(s, out var g)) return new ProductId(g);
        }
        throw new JsonException($"Invalid ProductId value.");
    }

    public override void Write(Utf8JsonWriter writer, ProductId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
