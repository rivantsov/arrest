using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Arrest.Tests {

  [Route("api/testdata")]
  [ApiController]
  public class TestDataController : ControllerBase {
    static int _newId;

    [HttpGet("items")]
    public IList<DataItem> GetItems() {
      return new DataItem[] {
        new DataItem { Id = 1, Name = "Name1", SomeDate = DateTime.Now },
        new DataItem { Id = 2, Name = "Name2", SomeDate = DateTime.Now },
        new DataItem { Id = 3, Name = "Name3", SomeDate = DateTime.Now },
      };
    }

    [HttpGet("items/{id}")]
    public DataItem GetItem(int id) {
      return  new DataItem { Id = id, Name = "Name" + id, SomeDate = DateTime.Now };
    }

    [HttpPost("items")]
    public DataItem CreateItem(DataItem item) {
      // pretend we created it in database  
      item.Id = ++_newId; 
      item.Version++;
      return item; 
    }

    [HttpPut("items")]
    public ObjectResult UpdateItem(DataItem item) {
      // pretend that we do some validation and return BadRequest if data is invalid
      if (item.SomeDate < new DateTime(1900, 1, 1)) {
        return BadRequest("Date is invalid");
      }
      // success
      item.Version++;
      return Ok(item);
    }


    [HttpGet("stringvalue")]
    public string GetStringValue() {
      return "This is string"; 
    }


  }
}
