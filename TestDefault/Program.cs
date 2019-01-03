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
