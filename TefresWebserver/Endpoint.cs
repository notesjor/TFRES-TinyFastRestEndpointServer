#region

using System;
using System.Threading.Tasks;

#endregion

namespace Tfres
{
  /// <summary>
  ///   Assign a method handler for when requests are received matching the supplied method and path.
  /// </summary>
  public class Endpoint
  {
    #region Constructors-and-Factories

    /// <summary>
    ///   Create a new route object.
    /// </summary>
    /// <param name="verb">The HTTP method, i.e. GET, PUT, POST, DELETE, etc.</param>
    /// <param name="path">The raw URL, i.e. /foo/bar/.  Be sure this begins and ends with '/'.</param>
    /// <param name="handler">The method that should be called to handle the request.</param>
    public Endpoint(HttpVerb verb, string path, Func<HttpContext, Task> handler)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
      if (handler == null) throw new ArgumentNullException(nameof(handler));

      Verb = verb;

      Path = path.ToLower();
      if (!Path.StartsWith("/")) Path = "/" + Path;
      if (!Path.EndsWith("/")) Path = Path  + "/";

      Handler = handler;
    }

    #endregion

    #region Public-Members

    /// <summary>
    ///   The HTTP method, i.e. GET, PUT, POST, DELETE, etc.
    /// </summary>
    public HttpVerb Verb { get; set; }

    /// <summary>
    ///   The raw URL, i.e. /foo/bar/.  Be sure this begins and ends with '/'.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    ///   The
    /// </summary>
    public Func<HttpContext, Task> Handler { get; }

    #endregion

    #region Private-Members

    #endregion

    #region Public-Methods

    #endregion

    #region Private-Methods

    #endregion
  }
}