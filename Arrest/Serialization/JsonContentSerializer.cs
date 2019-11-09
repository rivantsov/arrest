using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Collections.ObjectModel;

namespace Arrest.Json  {

  public class JsonContentSerializer : IContentSerializer {
    // Some APIs follow this draft for errors in REST APIs: https://tools.ietf.org/html/draft-nottingham-http-problem-07
    // So returned error is JSon but content type is problem+json
    public const string JsonErrorMediaType = "application/problem+json";
    public const string JsonMediaType = "application/json";
    public readonly JsonSerializer JsonSerializer;
    public readonly JsonSerializerSettings SerializerSettings;
    public bool Indent = true;

    public JsonContentSerializer(JsonNameMapping nameMapping = JsonNameMapping.Default, JsonSerializerSettings settings = null) {
      if (settings == null) {
        settings = new JsonSerializerSettings();
        settings.Formatting = Formatting.Indented; 
        settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter()); //serialize enum as names, not numbers
      }
      SerializerSettings = settings;
      if (SerializerSettings.ContractResolver == null) {
        SerializerSettings.ContractResolver = new JsonNameContractResolver(nameMapping); //to process NodeAttribute attribute
      }
      JsonSerializer = JsonSerializer.Create(SerializerSettings);
    }

    public string ContentTypes {
      get {
        return JsonMediaType + ", " + JsonErrorMediaType;
      }
    }

    public object Deserialize(Type type, string content) {
      var reader = new StringReader(content); 
      var obj = JsonSerializer.Deserialize(reader, type);
      return obj;
    }

    public string Serialize(object value) {
      if (value == null)
        return null; 
      var stream = new MemoryStream();
      if (value != null) {
        var writer = new StreamWriter(stream);
        JsonSerializer.Serialize(writer, value);
        writer.Flush();
      }
      //Get raw content
      stream.Position = 0;
      var reader = new StreamReader(stream);
      var raw = reader.ReadToEnd();
      return raw; 
    }

  }//class
}
