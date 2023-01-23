#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
      _routes = new Dictionary<HttpMethod, Dictionary<string,Action<HttpContext>>>();
      _lock = new object();
    }

    #endregion

    #region Private-Members

    private readonly Dictionary<HttpMethod, Dictionary<string,Action<HttpContext>>> _routes;
    private readonly object _lock;

    #endregion

    #region Public-Methods

    /// <summary>
    ///   Add a route.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">URL path, i.e. /path/to/resource.</param>
    /// <param name="handler">Method to invoke.</param>
    public void Add(HttpMethod method, string path, Action<HttpContext> handler)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
      if (handler == null) throw new ArgumentNullException(nameof(handler));

      path = path.ToLower();
      if (!path.StartsWith("/")) path = "/" + path;
      if (!path.EndsWith("/")) path = path  + "/";
      
      if(Exists(method, path)) return;

      lock (_lock)
      {
        if (!_routes.ContainsKey(method)) 
          _routes.Add(method, new Dictionary<string, Action<HttpContext>>());
        
        _routes[method].Add(path, handler);
      }
    }

    /// <summary>
    ///   Check if a static route exists.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">URL path.</param>
    /// <returns>True if exists.</returns>
    public bool Exists(HttpMethod method, string path)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

      path = path.ToLower();
      if (!path.StartsWith("/")) path = "/" + path;
      if (!path.EndsWith("/")) path = path  + "/";

      lock (_lock) 
        return _routes.ContainsKey(method) && _routes[method].ContainsKey(path);
    }

    /// <summary>
    ///   Match a request method and URL to a handler method.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">URL path.</param>
    /// <returns>Method to invoke.</returns>
    public Action<HttpContext> Match(HttpMethod method, string path)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

      path = path.ToLower();
      if (!path.StartsWith("/")) path = "/" + path;
      if (!path.EndsWith("/")) path = path  + "/";

      lock (_lock) return
        _routes.ContainsKey(method) && _routes[method].ContainsKey(path) 
          ? _routes[method][path] 
          : null;
    }

    #endregion
  }
}