# TFRES-TinyFastRestEndpointServer

A tiny fast REST-Server written in C# async. 

## Important Notes
- TFRES only supports static Endpoints (called: static Routes in Watson Webserver).
- TFRES based now on WatsonWebserver 3.x - So you need to change your signatures from ''HttpResponse OldFunc(HttpRequest arg)'' to ''Task NewFunc(HttpContext ctx)''.
  
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
        s.AddEndpoint(HttpVerb.POST, "/sayHello/", PostSayHello);
	s.AddEndpoint(HttpVerb.GET, "/download/", GetBigFileStream);
  
        Console.WriteLine("Press ENTER to exit");
        Console.ReadLine();
      }
  
      static Task DefaultRoute(HttpContext ctx)
        => ctx.Response.Send(200); // send only HTTP-StatusCode
  
      static Task GetHelloRoute(HttpContext ctx)
        => ctx.Response.Send(200, "Hello from the GET /hello static route!"); // send StatusCode and Text-Message

      private static Task GetJsonObjRoute(HttpContext ctx)
        => ctx.Response.Send(new Person { Name = "Jan", Animals = 1 }); // send an object JSON serialized
      
      static Task PostSayHello(HttpContext ctx)
        => new HttpResponse(ctx, true, 200, null, "text/plain", $"Hello {ctx.PostData<Person>().Name}!");
		
      static Task GetBigFileStream(HttpContext ctx){
        // Upload
        if (ctx.Request.ChunkedTransfer)
        {
          bool finalChunk = false;
          while (!finalChunk)
          {
            Chunk chunk = await ctx.Request.ReadChunk();
            // work with chunk.Length and chunk.Data (byte[])
            finalChunk = chunk.IsFinalChunk;
          }
        }
        else
        {
          // read from ctx.Request.Data stream   
        }

        // Download
        var buffer = new byte[65536];
        var size = 0;
        using (var fs = new FileStream("download.mp4", FileMode.Open, FileAccess.Read)) // you need a download.mp4 file
        {
          size = fs.Read(buffer, 0, buffer.Length);
          while (size > 0)
          {
            arg.Response.SendChunk(buffer).Wait();
            size = fs.Read(buffer, 0, buffer.Length);
          }
          return arg.Response.SendFinalChunk(buffer);
        }
      }
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
