using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Arrest;
using Arrest.Sync;

// This is verification app for SyncAsync bridge, to ensure that SyncAsync.RunSync methods work OK, 
//  without deadlocka under ASP.NET and ASP.NET core. Just run this web app and hit the URL 
//     api/syncasynctest/utcnow
// to invoke the GetUtcNow method. The method should execute without deadlocks and return current UTC. 
// The sync versions of RestClient use SyncAsync.RunSync, so we check here that they work OK. 

namespace AspNetCoreSyncAsyncTest {
  using JsonObject = System.Collections.Generic.IDictionary<string, string>;

  [Route("api/syncasynctest")]
  [ApiController]
  public class SyncAsyncTestController : ControllerBase {

    const string WorldClockUrl = "http://worldclockapi.com/api/json/utc/now";

    [HttpGet("UtcNow")]
    public string GetUtcNow() {
      //  ---- Methods that do NOT work ------------
      // var res = QueryUtcAsync().Result; //deadlocks
      // var res = QueryUtcAsync().ConfigureAwait(false).GetAwaiter().GetResult(); // deadlocks
      // var res = SyncAsync.RunSync(() => QueryUtcAsync()); //works OK

      // The following call is a test for non-generic overload of RunSync 
      Arrest.SyncAsync.RunSync(() => Delay(20));
      // Call method that will call non-async method restClient.Get<>
      var res = QueryUtc(); // works OK
      return $"Success! - sync->async call did not deadlock. Result UTC now: {res}";
    }

    private string QueryUtc() {
      var client = new RestClient(WorldClockUrl);
      var resp = client.Get<JsonObject>(string.Empty);
      return resp["currentDateTime"];
    }

    // For commented out test calls that do NOT work
    private async Task<string> QueryUtcAsync() {
      var client = new RestClient(WorldClockUrl);
      var resp = await client.GetAsync<JsonObject>(string.Empty);
      return resp["currentDateTime"];
    }

    // to test another overload of RunSync, non-generic for Actions (not funcs)
    private async Task Delay(int ms) {
      await Task.Delay(ms);
    }
  }
}
