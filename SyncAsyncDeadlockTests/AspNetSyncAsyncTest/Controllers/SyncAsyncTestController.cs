using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Arrest;
using Arrest.Sync;

// This is verification app for SyncAsync bridge, to ensure that SyncAsync.RunSync methods work OK, 
//  without deadlocka under ASP.NET and ASP.NET core. Just run this web app and hit the URL 
//     api/syncasynctest/utcnow
// to invoke the GetUtcNow method. The method should execute without deadlocks and return current UTC. 
// The sync versions of RestClient use SyncAsync.RunSync, so we check here that they work OK. 

namespace AspNetSyncAsyncTest.Controllers {
  using JsonObject = System.Collections.Generic.IDictionary<string, string>;

  public class SyncAsyncTestController : ApiController {

    [HttpGet]
    public string GetUtcNow() {
      // var res = QueryUtcAsync().Result; //deadlocks
      // var res = QueryUtcAsync().ConfigureAwait(false).GetAwaiter().GetResult(); // deadlocks
      // var res = SyncAsync.RunSync(() => QueryUtcAsync()); //works OK
      var res = QueryUtc(); // works OK
      return $"Success! - sync->async call did not deadlock. Result UTC now: {res}";
    }


    const string WorldClockUrl = "http://worldclockapi.com/api/json/utc/now";

    private string QueryUtc() {
      var client = new RestClient(WorldClockUrl);
      var resp = client.Get<JsonObject>(string.Empty);
      return resp["currentDateTime"];
    }

    private async Task<string> QueryUtcAsync() {
      var client = new RestClient(WorldClockUrl);
      var resp = await client.GetAsync<JsonObject>(string.Empty);
      return resp["currentDateTime"];
    }

  }
}
