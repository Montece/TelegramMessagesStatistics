using System.Text.Json.Serialization;

namespace TelegramMessagesStatistics;

internal sealed class TelegramMessage
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; set; }


    [JsonPropertyName("out")]
    public bool? Outbound { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("from_id")]
    public string? FromId { get; set; }
}