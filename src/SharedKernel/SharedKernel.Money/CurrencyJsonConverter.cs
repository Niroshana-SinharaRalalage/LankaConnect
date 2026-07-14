using System.Text.Json;
using System.Text.Json.Serialization;

namespace LankaConnect.SharedKernel.Money;

/// <summary>
/// Sprint-Day 7 (2026-07-14, Consult #25 attack order (c)): serialize <see cref="Currency"/>
/// as its ISO 4217 3-letter code string (e.g. <c>"USD"</c>) instead of the default
/// object shape (<c>{code, name, symbol, decimalDigits}</c>).
/// </summary>
/// <remarks>
/// <para>
/// Rationale: the Wave 9 API smoke suite (baseline 182/0/79 pre-sprint) treats the
/// smoke as the API contract — <c>Smoke-EventDetailPriceShape.ps1</c> asserts
/// <c>ticketPriceCurrency</c> matches <c>^[A-Z]{3}$</c>. The frontend consumes the
/// same shape. Post-Consult-#20 sweep, DTOs typed as <c>Currency?</c> started
/// leaking the full VO into JSON responses (System.Text.Json's default behavior
/// serializes every public property), breaking the round-trip smoke.
/// </para>
/// <para>
/// Reading: accepts either a JSON string (canonical) OR the legacy object shape
/// <c>{"code": "USD", ...}</c> for backward compatibility during rollout. Both
/// resolve via <see cref="Currency.FromCode"/> — currencies are singletons.
/// </para>
/// </remarks>
public sealed class CurrencyJsonConverter : JsonConverter<Currency>
{
    public override Currency? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var code = reader.GetString();
            return string.IsNullOrWhiteSpace(code) ? null : Currency.FromCode(code);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Legacy-object fallback: {code:"USD", name:"...", ...}
            string? code = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propName = reader.GetString();
                    reader.Read();
                    if (propName != null && propName.Equals("code", StringComparison.OrdinalIgnoreCase))
                    {
                        code = reader.GetString();
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
            }
            return string.IsNullOrWhiteSpace(code) ? null : Currency.FromCode(code);
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} when reading Currency.");
    }

    public override void Write(Utf8JsonWriter writer, Currency value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.Code);
    }
}
