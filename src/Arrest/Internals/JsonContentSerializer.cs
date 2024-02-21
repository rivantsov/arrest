using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arrest.Internals {

  public interface IContentSerializer {
    object Deserialize(string json, Type type);
    string Serialize(object value);
  }

  public class JsonContentSerializer : IContentSerializer {
    public JsonSerializerOptions Options;

    public JsonContentSerializer(JsonSerializerOptions options = null) {
      this.Options = options; 
      if (Options == null) {
        Options = new JsonSerializerOptions() {
          IncludeFields = true,
          WriteIndented = true,
        };
        Options.Converters.Add(new JsonStringEnumConverter());
      }
    }

    public object Deserialize(string json, Type type) {
      var result = JsonSerializer.Deserialize(json, type, Options);
      return result; 
    }

    public string Serialize(object value) {
      if (value == null)
        return null; 
      var json = JsonSerializer.Serialize(value, Options);
      return json;
    }

  }//class
}
