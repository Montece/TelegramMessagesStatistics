using System.Text.Json.Serialization;

namespace TelegramMessagesStatistics;

internal sealed class TelegramExport
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? ChatType { get; set; }

    [JsonPropertyName("id")]
    public long? ChatId { get; set; }

    [JsonPropertyName("messages")]
    public List<TelegramMessage> Messages { get; set; } = new();
}