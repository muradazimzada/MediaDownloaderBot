//using System;
//using System.Text.Json;
//using Newtonsoft.Json;

//public class DateTimeConverter : JsonConverter<DateTime>
//{
//    public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
//    {
//        writer.WriteValue(new DateTimeOffset(value).ToUnixTimeSeconds());
//    }

//    public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
//    {
//        if (reader.TokenType == JsonTokenType.Integer)
//        {
//            long seconds = (long)reader.Value;
//            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
//        }
//        throw new JsonSerializationException("Invalid date format");
//    }
//}
