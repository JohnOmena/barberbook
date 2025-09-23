using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BarberBook.Api.Json;

/// <summary>
/// Serializa DateTimeOffset sempre no fuso configurado do Brasil (default America/Sao_Paulo),
/// emitindo string no formato ISO com offset (ex.: -03:00) e nunca com 'Z'.
/// </summary>
public sealed class BrazilDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private static readonly TimeZoneInfo Tz = ResolveBrazilTimeZone();

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Delega parsing padrão (aceita Z ou -03:00)
        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s)) return default;
        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
            return dto;
        // Fallback para DateTime sem offset, assume horário do Brasil
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
        {
            var local = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
            var offset = Tz.GetUtcOffset(local);
            return new DateTimeOffset(local, offset);
        }
        return default;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // Converte para horário do Brasil preservando a data local e escreve com offset (zzz)
        var local = TimeZoneInfo.ConvertTime(value.UtcDateTime, Tz);
        var offset = Tz.GetUtcOffset(local);
        var dto = new DateTimeOffset(local, offset);
        writer.WriteStringValue(dto.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture));
    }

    private static TimeZoneInfo ResolveBrazilTimeZone()
    {
        var envTz = Environment.GetEnvironmentVariable("BB_TIMEZONE")
                 ?? Environment.GetEnvironmentVariable("TZ")
                 ?? Environment.GetEnvironmentVariable("TIMEZONE");
        string[] ids = new[]
        {
            envTz,
            "America/Sao_Paulo",
            "America/Maceio",
            "E. South America Standard Time",
            "SA Eastern Standard Time"
        };
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "BRT", "BRT");
    }
}

