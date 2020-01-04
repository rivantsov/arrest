using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arrest.TestService {

  public class DataItem {
    public string Name;
    public int Id;
    public DateTime SomeDate;
    public int Version; //increments each time we update item
  }

  public class SoftError {
    public string Code;
    public string Message;
  }

}
