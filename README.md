# TFRES-TinyFastRestEndpointServer

A tiny fast REST-Server written in C# async. 

## Important Notes
- TFRES only supports static Endpoints (called: static Routes in Watson Webserver).
  
## Example using Routes
```
using WatsonWebserver;

static void Main(string[] args)
{
   Server s = new Server("127.0.0.1", 9000, DefaultRoute);

   // add static routes
   s.AddEndpoint("get", "/hello/", GetHelloRoute);
   s.AddEndpoint("get", "/world/", GetWorldRoute);
   
   Console.WriteLine("Press ENTER to exit");
   Console.ReadLine();
}

static HttpResponse GetHelloRoute(HttpRequest req)
{
   return new HttpResponse(req, true, 200, null, "text/plain", "Hello from the GET /hello static route!", true);
}

static HttpResponse GetWorldRoute(HttpRequest req)
{
   return new HttpResponse(req, true, 200, null, "text/plain", "Hello from the GET /world static route!", true);
}

static HttpResponse DefaultRoute(HttpRequest req)
{
   return new HttpResponse(req, true, 200, null, "text/plain", "Hello from the default route!", true);
}
```

## Running under Mono
Watson works well in Mono environments to the extent that we have tested it. It is recommended that when running under Mono, you execute the containing EXE using --server and after using the Mono Ahead-of-Time Compiler (AOT).

NOTE: Windows accepts '0.0.0.0' as an IP address representing any interface.  On Mac and Linux you must be specified ('127.0.0.1' is also acceptable, but '0.0.0.0' is NOT).

```
mono --aot=nrgctx-trampolines=8096,nimt-trampolines=8096,ntrampolines=4048 --server myapp.exe
mono --server myapp.exe
```
