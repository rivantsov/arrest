using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Arrest {

  public interface IContentSerializer {
    string ContentTypes { get; }
    object Deserialize(Type type, string content);
    string Serialize(object value);
  }

}
