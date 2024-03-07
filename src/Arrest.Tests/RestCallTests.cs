using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arrest.Internals;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Arrest.Tests {

  [TestClass]
  public class RestCallTests {

    [TestInitialize]
    public void Init() {
      TestEnv.EnsureInitialized();
    }

    [TestMethod]
    public async Task TestRestClientAsync() {
      var client = new RestClient(TestEnv.ServiceUrl + "/api/testdata");

      // Get many
      var items = await client.GetAsync<DataItem[]>("items");
      Assert.AreEqual(3, items.Length);
      // Get one; for any id value, the test service returns an item with this Id
      var id = 5;
      var item = await client.GetAsync<DataItem>("items/{0}", id);
      Assert.AreEqual(id, item.Id);

      // Post
      var newItem = new DataItem() { Name = "NEW_ITEM", SomeDate = DateTime.UtcNow };
      var newItemBack = await client.PostAsync<DataItem, DataItem>(newItem, "items");
      Assert.IsTrue(newItemBack.Id > 0, "Expected Id > 0");
      Assert.IsTrue(newItemBack.Version > 0, "Expected version > 0");

      // update/put
      var currV = newItemBack.Version;
      newItemBack.Name += "_U";
      newItemBack = await client.PutAsync<DataItem, DataItem>(newItemBack, "items");
      Assert.AreEqual(currV + 1, newItemBack.Version, "Expected version incremented");

      // try update with invalid date; the server will return BadRequest which will be 
      // converted to BadRequestException by the Rest client
      newItemBack.SomeDate = new DateTime(1800, 1, 1);
      try {
        newItemBack = await client.PutAsync<DataItem, DataItem>(newItemBack, "items");
        Assert.Fail("Expected BadRequest exception.");
      } catch (BadRequestException) {
        // this is successful catch, nothing to do
      }

      // get string as text
      var str = await client.GetStringAsync("stringvalue");
      Assert.AreEqual("This is string", str);
    }

    [TestMethod]
    public void TestRestClientSync() {
      var client = new RestClient(TestEnv.ServiceUrl + "/api/testdata");

      // Get many
      var items = client.Get<DataItem[]>("items");
      Assert.AreEqual(3, items.Length);
      // Get one; for any id value, the test service returns an item with this Id
      var id = 5;
      var item = client.Get<DataItem>("items/{0}", id);
      Assert.AreEqual(id, item.Id);

      // Post
      var newItem = new DataItem() { Name = "NEW_ITEM", SomeDate = DateTime.UtcNow };
      var newItemBack = client.Post<DataItem, DataItem>(newItem, "items");
      Assert.IsTrue(newItemBack.Id > 0, "Expected Id > 0");
      Assert.IsTrue(newItemBack.Version > 0, "Expected version > 0");

      // update/put
      var currV = newItemBack.Version;
      newItemBack.Name += "_U";
      newItemBack = client.Put<DataItem, DataItem>(newItemBack, "items");
      Assert.AreEqual(currV + 1, newItemBack.Version, "Expected version incremented");

      // try update with invalid date; the server will return BadRequest which will be 
      // converted to BadRequestException by the Rest client, and response body will be deserialized
      // into SoftError[] array. 
      newItemBack.SomeDate = new DateTime(1800, 1, 1);
      try {
        newItemBack = client.Put<DataItem, DataItem>(newItemBack, "items");
        Assert.Fail("Expected BadRequest exception.");
      } catch (BadRequestException) {
        // this is successful catch, nothing to do
      }

      // get string as text
      var str = client.GetString("stringvalue");
      Assert.AreEqual("This is string", str);
    }

    // Demonstrates/tests building URL query part from properties of an object (search parameters)
    [TestMethod]
    public void TestBuildUrlQuery() {
      var queryParam = new QueryParameters() {
        StrField = "a", StrProp = "b", IntField = 123
      };
      // Build Query part of URL from properties of an object (URL query is all after '?')
      var query = RestUtility.BuildUrlQueryFromObject(queryParam);
      Assert.AreEqual("StrProp=b&StrField=a&IntField=123", query);
      Debug.WriteLine($"result: {query}");
    }

    // Sample query parameters class for test; mix fields and props, 
    // also add static and instance methods to make sure they don't break anything
    class QueryParameters {
      public string StrField;
      public string StrProp { get; set; }
      public int? IntField;
      public DateTime? DateProp { get; set; }

      public static int Foo() => 3;
      public int Bar() => 5;
    }

    [TestMethod]
    public async Task TestSpecialArguments() {
      // we are testing custom headers that are provided to a single call; for example X-CorrelationId header.
      // also cancellation token, and ArgContextBox (to get response header)
      const string HeaderName = "X-CorrelationId";
      var tokenSource = new CancellationTokenSource();
      var token = tokenSource.Token; 

      var client = new RestClient(TestEnv.ServiceUrl + "/api/testdata");
      var respBox = new ArgContextBox(); 
      var corrId = Guid.NewGuid().ToString();
      
      var corrIdBack = await client.GetAsync<string>("echocorrelationid", (HeaderName, corrId), respBox, token);
      Assert.AreEqual(corrId, corrIdBack); //returned as function result
      var respHeaderValue = respBox.GetResponseHeader(HeaderName)?.FirstOrDefault();
      Assert.AreEqual(corrId, respHeaderValue);
    }

    [TestMethod]
    public async Task TestRetries() {
      var retry = new RetryPolicy(0.5, 0.5, 0.5, 0.5); //retry 4 times with interval 0.5 seconds
      var client = new RestClient(TestEnv.ServiceUrl + "/api/testdata", retryPolicy: retry );
      // server throws errors - 3 times, but client retries and finally 
      var respOK = await client.GetAsync<string>("make-server-error");
      Assert.AreEqual("OK", respOK);
    }

  }
}
