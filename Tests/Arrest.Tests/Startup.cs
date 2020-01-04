using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Arrest.Tests {

  public static class Startup {

    // using IP address instead of localhost to view traffic in Fiddler
    public const string ServiceUrl = "http://127.0.0.1:60285"; 

    static IWebHost _webHost;
    public static void StartService() {
      if (_webHost != null)
        return; 
      var hostBuilder = WebHost.CreateDefaultBuilder()
          .ConfigureAppConfiguration((context, config) => { })
          .UseStartup<Arrest.TestService.Startup>()
          .UseUrls(ServiceUrl);
      _webHost = hostBuilder.Build();

      Task.Run(() => _webHost.Run());
      Debug.WriteLine("The service is running on URL: " + ServiceUrl);
    }


  }
}
