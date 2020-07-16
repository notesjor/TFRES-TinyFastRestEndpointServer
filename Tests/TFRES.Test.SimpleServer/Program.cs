using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Tfres;

namespace TFRES.Test.SimpleServer
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.Write("Start Server...");
      // Starte Server lokal auf Port 9999
      var server = new Server("127.0.0.1", 9999, (ctx) => ctx.Response.Send(200));

      // Einfache Endpunkte mit direkter Antwort
      server.AddEndpoint(HttpVerb.GET, "/hello", (ctx) => ctx.Response.Send("Hello World"));
      server.AddEndpoint(HttpVerb.GET, "/user", (ctx) => ctx.Response.Send(new User { Name = ctx.Request.GetData()["name"] }));

      // Post Endpunkt mit komplexer Antwort
      server.AddEndpoint(HttpVerb.POST, "/check", AgeCheck);

      // Post Endpunkt mit Stream Antwort
      server.AddEndpoint(HttpVerb.GET, "/corpus", GetBigCorpusStream);

      Console.WriteLine("ok!");
      Console.ReadLine();
    }

    private static Task AgeCheck(HttpContext ctx)
    {
      var user = ctx.PostData<User>(); // Automatisch Deserialisierung eines JSON-Objekts
      if (user == null || string.IsNullOrEmpty(user.Name))
        return ctx.Response.Send(HttpStatusCode.InternalServerError,
                          "Nutzer darf nicht null sein und die Eigenschaft 'name' muss gesetzt sein.");

      if (user.Age < 18)
        return ctx.Response.Send(HttpStatusCode.InternalServerError,
                                 "Nutzer muss mindestens 18 Jahre alt sein");

      return ctx.Response.Send(HttpStatusCode.Accepted);
    }

    private static Task GetBigCorpusStream(HttpContext ctx)
    {
      var buffer = new byte[65536]; // Lese aus lokaler Datei 'corpus.cec6' mit einem 64KB Buffer
      using (var fs = new FileStream("corpus.cec6", FileMode.Open, FileAccess.Read))
      {
        var size = fs.Read(buffer, 0, buffer.Length);
        while (size > 0)
        {
          ctx.Response.SendChunk(buffer).Wait(); // Sende 'buffer' als Chunk via HTTP
          size = fs.Read(buffer, 0, buffer.Length); // Lese nächsten 'buffer' ein
        }
        return ctx.Response.SendFinalChunk(buffer); // Schließe Verbindung.
      }
    }
  }

  [Serializable]
  public class User
  {
    public string Name { get; set; }
    public int Age { get; set; } = (new Random()).Next(18, 99);
  }
}