using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http.Headers;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading;

namespace Arrest  {

  public interface IContentSerializer {
    object Deserialize(string json, Type type);
    string Serialize(object value);
  }



  public class JsonContentSerializer : IContentSerializer {
    public JsonSerializerOptions Options;

    public JsonContentSerializer(JsonSerializerOptions options = null) {
      if (options == null) {
        options = new JsonSerializerOptions() {
          IncludeFields = true, 
          WriteIndented = true,          
        };
        options.Converters.Add(new JsonStringEnumConverter());
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
