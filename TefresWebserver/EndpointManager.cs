#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Tfres
{
  internal class EndpointManager
  {
    #region Constructors-and-Factories

    public EndpointManager()
    {
      _routes = new List<Endpoint>();
      _routeLock = new object();
    }

    #endregion

    #region Private-Methods

    private void Add(Endpoint route)
    {
      if (route == null) throw new ArgumentNullException(nameof(route));

      route.Verb = route.Verb;
      route.Path = route.Path.ToLower();
      if (!route.Path.StartsWith("/")) route.Path = "/"      + route.Path;
      if (!route.Path.EndsWith("/")) route.Path = route.Path + "/";

      if (Exists(route.Verb, route.Path)) return;

      lock (_routeLock)
      {
        _routes.Add(route);
      }
    }

    #endregion

    #region Public-Members

    #endregion

    #region Private-Members

    private readonly List<Endpoint> _routes;
    private readonly object _routeLock;

    #endregion

    #region Public-Methods

    public void Add(HttpVerb verb, string path, Func<HttpRequest, HttpResponse> handler)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
      if (handler == null) throw new ArgumentNullException(nameof(handler));

      var r = new Endpoint(verb, path, handler);
      Add(r);
    }

    private bool Exists(HttpVerb verb, string path)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

      path = path.ToLower();
      if (!path.StartsWith("/")) path = "/" + path;
      if (!path.EndsWith("/")) path = path  + "/";

      lock (_routeLock)
      {
        var curr = _routes.FirstOrDefault(i => i.Verb == verb && i.Path == path);
        if (curr == null) return false;
      }

      return true;
    }

    public Func<HttpRequest, HttpResponse> Match(HttpVerb verb, string path)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

      path = path.ToLower();
      if (!path.StartsWith("/")) path = "/" + path;
      if (!path.EndsWith("/")) path = path  + "/";

      lock (_routeLock)
      {
        return _routes.FirstOrDefault(i => i.Verb == verb && i.Path == path)?.Handler;
      }
    }

    #endregion
  }
}