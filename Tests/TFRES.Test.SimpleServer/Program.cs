using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using Tfres;

namespace TFRES.Test.SimpleServer
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.Write("Start Server...");
      var server = new Server("127.0.0.1", 9999, (ctx) => ctx.Response.Send(200));
      server.AddEndpoint(HttpVerb.GET, "/hello", (ctx) => ctx.Response.Send("Hello World"));
      server.AddEndpoint(HttpVerb.GET, "/user", (ctx) => ctx.Response.Send(new User { Name = ctx.Request.GetData()["name"] }));
      Console.WriteLine("ok!");
      Console.WriteLine("Start Tests...ok!");

      var clients = 10;
      var stop = DateTime.Now.AddSeconds(60);
      var results = new ConcurrentBag<Status>();

      Parallel.For(0, clients, new ParallelOptions { MaxDegreeOfParallelism = clients }, i =>
       {
         var watch = new Stopwatch();
         while (DateTime.Now < stop)
         {
           watch.Restart();
           //var code = TestDefaultRoute();
           //var code = TestHelloRoute();
           var code = TestUserRoute();
           //var code = TestUserRoute2();
           //var code = TestOwnRoute();
           watch.Stop();

           results.Add(new Status
           {
             Client = i,
             Success = code == 200,
             ElapsedMilliseconds = watch.ElapsedMilliseconds
           });
         }
       });

      Console.Clear();
      Console.WriteLine($"TOTAL: {results.Count(x => x.Success)} ok / {results.Count(x => !x.Success)} error => {results.Count} total");
      var success = results.Where(x => x.Success).ToArray();
      Console.WriteLine($"MIN: {success.Min(x=>x.ElapsedMilliseconds)} / MAX: {success.Max(x => x.ElapsedMilliseconds)} / AVG: {success.Average(x => x.ElapsedMilliseconds)}");
      Console.WriteLine("\n---\n");

      for (var i = 0; i < clients; i++)
      {
        var sub = results.Where(x => x.Client == i).ToArray();
        Console.WriteLine($"Client {i:D2}: {sub.Count(x => x.Success)} ok / {sub.Count(x => !x.Success)} error => {sub.Count()} total");
        success = sub.Where(x => x.Success).ToArray();
        Console.WriteLine($"MIN: {success.Min(x => x.ElapsedMilliseconds)} / MAX: {success.Max(x => x.ElapsedMilliseconds)} / AVG: {success.Average(x => x.ElapsedMilliseconds)}");
        Console.WriteLine();
      }

      Console.ReadLine();
    }

    private static int TestDefaultRoute()
    {
      var client = new RestClient("http://127.0.0.1:9999/");
      var request = new RestRequest(Method.GET);
      return (int)client.Execute(request).StatusCode;
    }

    private static int TestHelloRoute()
    {
      var client = new RestClient("http://127.0.0.1:9999/hello");
      var request = new RestRequest(Method.GET);
      return (int)client.Execute(request).StatusCode;
    }

    private static int TestUserRoute()
    {
      var client = new RestClient("http://127.0.0.1:9999/user");
      var request = new RestRequest(Method.GET);
      request.AddParameter("name", "Jan");
      var res = client.Execute(request);
      return (int)res.StatusCode;
    }

    private static int TestUserRoute2()
    {
      try
      {
        var client = new RestClient("http://127.0.0.1:9999/user");
        var request = new RestRequest(Method.GET);
        request.AddParameter("name", "Jan");
        var res = client.Execute(request);
        return (int) res.StatusCode                                  != 200   ? 500 :
               JsonConvert.DeserializeObject<User>(res.Content).Name == "Jan" ? 200 : 500;
      }
      catch
      {
        return 500;
      }
    }

    private static int TestOwnRoute()
    {
      var client = new RestClient("http://127.0.0.1:9999/execute");
      var request = new RestRequest(Method.POST);
      request.AddParameter("undefined", "{\"action\": \"frequency1\", \"arguments\": [\"Wort\"]}", ParameterType.RequestBody);
      return (int)client.Execute(request).StatusCode;
    }

    private class Status
    {
      public int Client { get; set; }
      public bool Success { get; set; }
      public long ElapsedMilliseconds { get; set; }
    }
  }

  [Serializable]
  public class User
  {
    public string Name { get; set; }
    public int Age { get; set; } = (new Random()).Next(18, 89);
    public Guid Guid { get; set; } = Guid.NewGuid();
  }
}
