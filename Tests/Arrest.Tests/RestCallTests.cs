using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Arrest.TestService;
using Arrest;
using Arrest.Sync;
using System.Diagnostics;
using System.Globalization;
using Arrest.Internals;

namespace Arrest.Tests {

  [TestClass]
  public class RestCallTests {

    [TestInitialize]
    public void Init() {
      Startup.StartService(); 
    }

    [TestMethod]
    public async Task TestRestClientAsync() {
      var client = new RestClient(Startup.ServiceUrl + "/api/testdata");

      // Get many
      var items = await client.GetAsync<DataItem[]>("items");
      Assert.AreEqual(3, items.Length);
      // Get one; for any id value, the test service returns an item with this Id
      var id = 5;
      var item = await client.GetAsync<DataItem>("items/{0}", id);
      Assert.AreEqual(id, item.Id);

      // Post, Put
      var newItem = new DataItem() { Name = "NEW_ITEM", SomeDate = DateTime.UtcNow };
      var newItemBack = await client.PostAsync<DataItem, DataItem>(newItem, "items");
      Assert.IsTrue(newItemBack.Id > 0, "Expected Id > 0");
      Assert.IsTrue(newItemBack.Version > 0, "Expected version > 0");
      // update/put
      var currV = newItemBack.Version;
      newItemBack.Name += "_U";
      newItemBack = await client.PutAsync<DataItem, DataItem>(newItemBack, "items");
      Assert.AreEqual(currV + 1, newItemBack.Version, "Expected version incremented");



      // get string as text
      var str = await client.GetStringAsync("stringvalue");
      Assert.AreEqual("This is string", str);
    }

    [TestMethod]
    public void TestUrlFormattingWithObject() {

      RestClientHelper.FormatAsUrlQuery(sample); 

      Debug.WriteLine($"result: {url}");
    }


  }
}
