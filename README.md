# TFRES-TinyFastRestEndpointServer

A tiny fast REST-Server written in C# async. 

## Important Notes
- TFRES only supports static Endpoints (called: static Routes in Watson Webserver).
  
## Example using Routes
```
using System;
using Newtonsoft.Json;
using Tfres;

namespace Application
{
  static class Program
  {
    static void Main(string[] args)
    {
      Server s = new Server("127.0.0.1", 9000, DefaultRoute);

      // add static routes
      s.AddEndpoint(HttpVerb.GET, "/helloWorld/", GetHelloRoute);
      s.AddEndpoint(HttpVerb.GET, "/jsonObj/", GetJsonObjRoute);

      Console.WriteLine("Press ENTER to exit");
      Console.ReadLine();
    }

    static HttpResponse DefaultRoute(HttpRequest req)
      => new HttpResponse(req, true, 200, null, "text/plain", "Hello from the default route!");

    static HttpResponse GetHelloRoute(HttpRequest req)
      => new HttpResponse(req, true, 200, null, "text/plain", "Hello from the GET /hello static route!");

    private static HttpResponse GetJsonObjRoute(HttpRequest req) =>
      new HttpResponse(req, true, 200, null, "application/json", JsonConvert.SerializeObject(new Person { Name = "Jan", Animals = 1 }));
  }

  public class Person
  {
    public string Name { get; set; }
    public int Animals { get; set; }
  }
}
```

## Running under Mono
TFRES works well in Mono environments to the extent that we have tested it. It is recommended that when running under Mono, you execute the containing EXE using --server and after using the Mono Ahead-of-Time Compiler (AOT).

NOTE: Windows accepts '0.0.0.0' as an IP address representing any interface.  On Mac and Linux you must be specified ('127.0.0.1' is also acceptable, but '0.0.0.0' is NOT).

```
mono --aot=nrgctx-trampolines=8096,nimt-trampolines=8096,ntrampolines=4048 --server myapp.exe
mono --server myapp.exe
```
