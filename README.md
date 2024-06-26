# TFRES-TinyFastRestEndpointServer

A tiny fast REST-Server written in C# async. 

## Important Notes
- TFRES only supports static Endpoints (called: static Routes in Watson Webserver).
- TFRES based now on WatsonWebserver 3.x - So you need to change your signatures from ''HttpResponse OldFunc(HttpRequest arg)'' to ''Task NewFunc(HttpContext ctx)''.
  
## Example using Routes
```
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Tfres;

namespace TFRES.Test.SimpleServer
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.Write("Start Server...");
      // Start a Server at loclhost (127.0.0.1) on port 10101
      var server = new Server("127.0.0.1", 10101, DefaultRouteTest);

      // Simple Endpoints - direct answer
      server.AddEndpoint(HttpMethod.Get, "/hello", (ctx) => ctx.Response.Send("Hello World"));
      server.AddEndpoint(HttpMethod.Get, "/user", (ctx) => ctx.Response.Send(new User { Name = ctx.Request.GetData()["name"] }));
      // Age-Check Endpoint with POST-Data
      server.AddEndpoint(HttpMethod.Post, "/check", AgeCheck);
      // Extrem simple file streaming - chunked auto-transfer
      server.AddEndpoint(HttpMethod.Get, "/corpus", GetBigCorpusStream);
      // If you send a object as response - the object is auto-serialized with Newtonsoft JSON
      server.AddEndpoint(HttpMethod.Get, "/newUser", NewUser);
      // Open a WebSocket
      server.AddEndpoint(HttpMethod.Get, "/openSocket", OpenSocket);

      Console.WriteLine("ok!");
      Console.ReadLine();
    }

    private static List<EasyWebSocket> _sockets = new List<EasyWebSocket>();

    private static void OpenSocket(HttpContext context)
    {
      var socket = context.WebSocketEasy();
      if(socket == null)
        return;
      
      socket.MessageReceived += (s, msg) => Console.WriteLine($"Message received: {msg}");
      socket.Closed += (s, e) =>
      {
        _sockets.Remove(s as EasyWebSocket);
        Console.WriteLine("Socket closed");
      };
      _sockets.Add(socket);

      socket.Send("Hello from Server").Wait();

      // wait until socket is closed
      socket.Wait();
    }

    private static void DefaultRouteTest(HttpContext ctx)
    {
      Console.WriteLine(ctx.Request.FullUrl);
    }

    private static void AgeCheck(HttpContext ctx)
    {
      var user = ctx.PostData<User>(); // Automatisch Deserialisierung eines JSON-Objekts
      if (user == null || string.IsNullOrEmpty(user.Name))
      {
        ctx.Response.Send(HttpStatusCode.InternalServerError, 
                          "Error 105: user can not be empty - and needs a name (user.Name)",
                          501,
                          "http://help.com/url");
        return;
      }

      if (user.Age < 18)
      {
        ctx.Response.Send(HttpStatusCode.InternalServerError,
                         "Error U18: user needs to be over 18.",
                         518,
                         "http://terms.of/service");

      }

      ctx.Response.Send(HttpStatusCode.Accepted);
    }

    private static void GetBigCorpusStream(HttpContext ctx) 
      => ctx.Response.SendFile("/path/veryBig.file");

    private static void NewUser(HttpContext ctx) 
      => ctx.Response.Send(new User { Name = "Jan", Age = 18 + new Random().Next(1, 50) });
  }

  [Serializable]
  public class User
  {
    public string Name { get; set; }
    public int Age { get; set; } = (new Random()).Next(18, 99);
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
