using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Arrest.Json {

  public enum JsonNameMapping {
    Default,
    CamelCase,
    UnderscoreAllLower,
  }


  /// <summary>Handles NodeAttribute that specifies Json node name. 
  /// Provides conversion of names from model to Json and back according to NameMapping setting. </summary>
  /// <remarks>You can provide completely custom name for model property using <see cref="NodeAttributeName"/>.
  /// </remarks>
  public class JsonNameContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver {
    /// <summary> The name of a custom attribute that provides Json node name. </summary>
    /// <remarks>Usually JsonProperty attribute from NewtonSoft package is used. However, using this attribute
    /// requires referencing the NewtonSoft package from your business layer assembly, which might not be 
    /// desirable. This class provides you with alternative - you can use your own attribute for this purpose;
    /// all what is needed is that it must have Name property. Set this NodeAttributeName value to the name 
    /// of the custom attribute to use this facility. 
    /// </remarks>
    public string NodeAttributeName = "NodeAttribute";
    public readonly JsonNameMapping NameMapping; 

    public JsonNameContractResolver(JsonNameMapping nameMapping) {
      NameMapping = nameMapping;
      switch(nameMapping) {
        case JsonNameMapping.Default:
          base.NamingStrategy = new DefaultNamingStrategy();
          break;
        case JsonNameMapping.CamelCase:
          base.NamingStrategy = new CamelCaseNamingStrategy();
          break;
        case JsonNameMapping.UnderscoreAllLower:
          base.NamingStrategy = new SnakeCaseNamingStrategy();
          break; 
      }
    }

    // Note: this method is called just once for a given property; Newtonsoft serializer caches metadata information,
    // so the result is cached and reused. We are not concerned with efficiency here
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
      var property = base.CreateProperty(member, memberSerialization);
      var nodeName = GetNodeName(member);
      if (!string.IsNullOrEmpty(nodeName))
        property.PropertyName = nodeName; 
      return property;
    }

    // Looks for NodeAttribute on member and returns its Name property if found.
    public string GetNodeName(MemberInfo member) {
      var attr = member.GetCustomAttributes(inherit: true).FirstOrDefault(a => a.GetType()
           .Name == this.NodeAttributeName);
      if (attr == null)
        return null;
      var propName = attr.GetType().GetProperty("Name");
      if (propName == null)
        return null;
      var name = propName.GetValue(attr) as string; 
      return name;

    }
  }// class
}
