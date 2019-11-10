using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Arrest.Xml {
  /// <summary>
  /// XML content serializer.
  /// </summary>
  /// <remarks>
  /// </remarks>
  public class XmlContentSerializer : IContentSerializer {
    public string ContentTypes { get; set; } = "application/xml";

    public object Deserialize(Type type, string content) {
      if (string.IsNullOrWhiteSpace(content))
        return null;
      var ser = new DataContractSerializer(type);
      using (var reader = new StringReader(content)) {
        var xmlReader = XmlReader.Create(reader);
        var obj = ser.ReadObject(xmlReader, false);
        return obj;
      }
    }

    public string Serialize(object value) {
      if (value == null)
        return null; 
      var ser = new XmlSerializer (value.GetType(), Type.EmptyTypes);
      using (var memStream = new MemoryStream()) {
        ser.Serialize(memStream, value);
        memStream.Flush();
        memStream.Position = 0;
        var reader = new StreamReader(memStream);
        var content = reader.ReadToEnd();
        return content;
      }
    }
  }
}
