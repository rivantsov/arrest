﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

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

    [HttpGet("echocorrelationid")]
    public string EchoCorelationId() {
      const string CorHeaderName = "X-CorrelationId";
      var headers = this.HttpContext.Request.Headers;
      headers.TryGetValue(CorHeaderName, out var sv);
      var corId = sv.ToString();
      // return corr Id as result, and also in response header
      var outHeaders = this.HttpContext.Response.Headers;
      outHeaders.TryAdd(CorHeaderName, new StringValues(corId));
      return corId;
    }


    [HttpGet("make-server-error")]
    public string MakeServerError() {
     if (ErrorsToThrowCount > 0) {
        ErrorsToThrowCount--;
        throw new Exception("Server error");
      }
      return "OK";
    }

    public static int ErrorsToThrowCount = 3; // decrements to zero
  }
}
