#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#endregion

namespace Tfres
{
  /// <summary>
  ///   Static route manager.  Static routes are used for requests using any HTTP method to a specific path.
  /// </summary>
  public class EndpointManager
  {
    #region Constructors-and-Factories

    /// <summary>
    ///   Instantiate the object.
    /// </summary>
    public EndpointManager()
    {
      _routes = new List<Endpoint>();
      _lock = new object();
    }

    #endregion

    #region Private-Members

    private readonly List<Endpoint> _routes;
    private readonly object _lock;

    #endregion

    #region Public-Methods

    /// <summary>
    ///   Add a route.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">URL path, i.e. /path/to/resource.</param>
    /// <param name="handler">Method to invoke.</param>
    public void Add(HttpVerb method, string path, Action<HttpContext> handler)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
      if (handler == null) throw new ArgumentNullException(nameof(handler));

      var r = new Endpoint(method, path, handler);
      Add(r);
    }

    /// <summary>
    ///   Remove a route.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">URL path.</param>
    public void Remove(HttpVerb method, string path)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

      var r = Get(method, path);
      if (r == null) return;

      lock (_lock) _routes.Remove(r);
    }

    /// <summary>
    ///   Retrieve a static route.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">URL path.</param>
    /// <returns>Endpoint if the route exists, otherwise null.</returns>
    public Endpoint Get(HttpVerb method, string path)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

      path = path.ToLower();
      if (!path.StartsWith("/")) path = "/" + path;
      if (!path.EndsWith("/")) path = path  + "/";

      lock (_lock) return _routes.FirstOrDefault(i => i.Verb == method && i.Path == path);
    }

    /// <summary>
    ///   Check if a static route exists.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">URL path.</param>
    /// <returns>True if exists.</returns>
    public bool Exists(HttpVerb method, string path)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

      path = path.ToLower();
      if (!path.StartsWith("/")) path = "/" + path;
      if (!path.EndsWith("/")) path = path  + "/";

      lock (_lock) return _routes.FirstOrDefault(i => i.Verb == method && i.Path == path) != null;
    }

    /// <summary>
    ///   Match a request method and URL to a handler method.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">URL path.</param>
    /// <returns>Method to invoke.</returns>
    public Action<HttpContext> Match(HttpVerb method, string path)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

      path = path.ToLower();
      if (!path.StartsWith("/")) path = "/" + path;
      if (!path.EndsWith("/")) path = path  + "/";

      lock (_lock) return _routes.FirstOrDefault(i => i.Verb == method && i.Path == path)?.Handler;
    }

    #endregion

    #region Private-Methods

    private void Add(Endpoint route)
    {
      if (route == null) throw new ArgumentNullException(nameof(route));

      route.Path = route.Path.ToLower();
      if (!route.Path.StartsWith("/")) route.Path = "/"      + route.Path;
      if (!route.Path.EndsWith("/")) route.Path = route.Path + "/";

      if (Exists(route.Verb, route.Path)) return;

      lock (_lock) _routes.Add(route);
    }

    #endregion
  }
}