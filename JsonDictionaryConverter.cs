using Newtonsoft.Json;

namespace LeFunnyAI {
  class JsonDictionaryConverter : JsonConverter {
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) { this.WriteValue(writer, value); }

    private void WriteValue(JsonWriter writer, object? value) {
      if (value == null) {
        writer.WriteNull();
        return;
      }

      if (value is IDictionary<string, object?> obj) {
        WriteObject(writer, obj);
        return;
      }
      if (value is IEnumerable<object?> array) {
        WriteArray(writer, array);
        return;
      }

      writer.WriteValue(value);
    }

    private void WriteObject(JsonWriter writer, IDictionary<string, object?> obj) {
      writer.WriteStartObject();
      foreach (var kvp in obj) {
        writer.WritePropertyName(kvp.Key);
        WriteValue(writer, kvp.Value);
      }
      writer.WriteEndObject();
    }

    private void WriteArray(JsonWriter writer, IEnumerable<object?> array) {
      writer.WriteStartArray();
      foreach (var o in array) {
        WriteValue(writer, o);
      }
      writer.WriteEndArray();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
      return ReadValue(reader);
    }

    private object? ReadValue(JsonReader reader) {
      while (reader.TokenType == JsonToken.Comment) {
        if (!reader.Read()) throw new JsonSerializationException("Unexpected Token when converting IDictionary<string, object>");
      }

      switch (reader.TokenType) {
        case JsonToken.StartObject:
          return ReadObject(reader);
        case JsonToken.StartArray:
          return ReadArray(reader);
        case JsonToken.Integer:
        case JsonToken.Float:
        case JsonToken.String:
        case JsonToken.Boolean:
        case JsonToken.Undefined:
        case JsonToken.Null:
        case JsonToken.Date:
        case JsonToken.Bytes:
          return reader.Value;
        default:
          throw new JsonSerializationException
              (string.Format("Unexpected token when converting IDictionary<string, object>: {0}", reader.TokenType));
      }
    }

    private object ReadArray(JsonReader reader) {
      List<object?> list = new();

      while (reader.Read()) {
        switch (reader.TokenType) {
          case JsonToken.Comment:
            break;
          default:
            var v = ReadValue(reader);

            list.Add(v);
            break;
          case JsonToken.EndArray:
            return list;
        }
      }

      throw new JsonSerializationException("Unexpected end when reading IDictionary<string, object>");
    }

    private object ReadObject(JsonReader reader) {
      Dictionary<string, object?> obj = new();

      while (reader.Read()) {
        switch (reader.TokenType) {
          case JsonToken.PropertyName:
            string propertyName = $"{reader.Value}";

            if (!reader.Read()) {
              throw new JsonSerializationException("Unexpected end when reading IDictionary<string, object>");
            }

            object? v = ReadValue(reader);
            obj[propertyName] = v;
            break;
          case JsonToken.Comment:
            break;
          case JsonToken.EndObject:
            return obj;
        }
      }

      throw new JsonSerializationException("Unexpected end when reading IDictionary<string, object>");
    }

    public override bool CanConvert(Type objectType) { return typeof(IDictionary<string, object>).IsAssignableFrom(objectType); }
  }
}