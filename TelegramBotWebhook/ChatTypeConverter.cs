using System;
using Newtonsoft.Json;
using Telegram.Bot.Types.Enums;

public class ChatTypeConverter : JsonConverter<ChatType>
{
    public override void WriteJson(JsonWriter writer, ChatType value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString().ToLower());
    }

    public override ChatType ReadJson(JsonReader reader, Type objectType, ChatType existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var value = (string)reader.Value;
        return value.ToLower() switch
        {
            "private" => ChatType.Private,
            "group" => ChatType.Group,
            "channel" => ChatType.Channel,
            "supergroup" => ChatType.Supergroup,
            "sender" => ChatType.Sender,
            _ => throw new JsonSerializationException($"Unknown chat type: {value}")
        };
    }
}
