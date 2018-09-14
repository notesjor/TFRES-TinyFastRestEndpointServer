using System;
using System.Collections.Generic;
using System.Linq;

namespace WatsonWebserver
{
  internal class EndpointManager
  {
    #region Constructors-and-Factories

    public EndpointManager()
    {
      Routes = new List<Endpoint>();
      RouteLock = new object();
    }

    #endregion

    #region Private-Methods

    private void Add(Endpoint route)
    {
      if (route == null) throw new ArgumentNullException(nameof(route));

      route.Verb = route.Verb.ToLower();
      route.Path = route.Path.ToLower();
      if (!route.Path.StartsWith("/")) route.Path = "/"      + route.Path;
      if (!route.Path.EndsWith("/")) route.Path = route.Path + "/";

      if (Exists(route.Verb, route.Path)) return;

      lock (RouteLock)
      {
        Routes.Add(route);
      }
    }

    #endregion

    #region Public-Members

    #endregion

    #region Private-Members

    private readonly List<Endpoint> Routes;
    private readonly object RouteLock;

    #endregion

    #region Public-Methods

    public void Add(string verb, string path, Func<HttpRequest, HttpResponse> handler)
    {
      if (string.IsNullOrEmpty(verb)) throw new ArgumentNullException(nameof(verb));
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
      if (handler == null) throw new ArgumentNullException(nameof(handler));

      var r = new Endpoint(verb, path, handler);
      Add(r);
    }

    public Endpoint Get(string verb, string path)
    {
      if (string.IsNullOrEmpty(verb)) throw new ArgumentNullException(nameof(verb));
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

      verb = verb.ToLower();
      path = path.ToLower();
      if (!path.StartsWith("/")) path = "/" + path;
      if (!path.EndsWith("/")) path = path  + "/";

      lock (RouteLock)
      {
        var curr = Routes.FirstOrDefault(i => i.Verb == verb && i.Path == path);
        if (curr == null || curr == default(Endpoint))
          return null;
        return curr;
      }
    }

    public bool Exists(string verb, string path)
    {
      if (string.IsNullOrEmpty(verb)) throw new ArgumentNullException(nameof(verb));
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

      verb = verb.ToLower();
      path = path.ToLower();
      if (!path.StartsWith("/")) path = "/" + path;
      if (!path.EndsWith("/")) path = path  + "/";

      lock (RouteLock)
      {
        var curr = Routes.FirstOrDefault(i => i.Verb == verb && i.Path == path);
        if (curr == null || curr == default(Endpoint)) return false;
      }

      return true;
    }

    public Func<HttpRequest, HttpResponse> Match(string verb, string path)
    {
      if (string.IsNullOrEmpty(verb)) throw new ArgumentNullException(nameof(verb));
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

      verb = verb.ToLower();
      path = path.ToLower();
      if (!path.StartsWith("/")) path = "/" + path;
      if (!path.EndsWith("/")) path = path  + "/";

      lock (RouteLock)
      {
        return Routes.FirstOrDefault(i => i.Verb == verb && i.Path == path)?.Handler;
      }
    }

    #endregion
  }
}