#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Tfres
{
  /// <summary>
  ///   Watson webserver.
  /// </summary>
  public class Server : IDisposable
  {
    #region Public-Methods

    /// <summary>
    ///   Tear down the server and dispose of background workers.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
    }

    #endregion

    #region Public-Members

    /// <summary>
    ///   Indicates whether or not the server is listening.
    /// </summary>
    public bool IsListening => _HttpListener?.IsListening ?? false;

    /// <summary>
    ///   Indicate the buffer size to use when reading from a stream to send data to a requestor.
    /// </summary>
    public int StreamReadBufferSize
    {
      get => _StreamReadBufferSize;
      set
      {
        if (value < 1) throw new ArgumentException("StreamReadBufferSize must be greater than zero.");
        _StreamReadBufferSize = value;
      }
    }

    /// <summary>
    ///   Static routes; i.e. routes with explicit matching and any HTTP method.
    /// </summary>
    public EndpointManager _endpoints = new EndpointManager();

    /// <summary>
    ///   Add a static route to the server.
    /// </summary>
    /// <param name="verb">The HTTP method, i.e. GET, PUT, POST, DELETE.</param>
    /// <param name="path">The raw URL to match, i.e. /foo/bar.</param>
    /// <param name="handler">The method to which control should be passed.</param>
    public void AddEndpoint(HttpVerb verb, string path, Func<HttpContext, Task> handler)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
      if (handler == null) throw new ArgumentNullException(nameof(handler));

      _endpoints.Add(verb, path, handler);
    }

    #endregion

    #region Private-Members

    private readonly EventWaitHandle _Terminator = new EventWaitHandle(false, EventResetMode.ManualReset);

    private readonly HttpListener _HttpListener;
    private readonly List<string> _ListenerHostnames;
    private readonly int _ListenerPort;
    private int _StreamReadBufferSize = 65536;

    private readonly Func<HttpContext, Task> _DefaultRoute;

    private readonly CancellationTokenSource _TokenSource;
    private readonly CancellationToken _Token;

    #endregion

    #region Constructor

    /// <summary>
    ///   Creates a new instance of the Watson Webserver.
    /// </summary>
    /// <param name="hostname">Hostname or IP address on which to listen.</param>
    /// <param name="port">TCP port on which to listen.</param>
    /// <param name="defaultRoute">
    ///   Method used when a request is received and no matching routes are found.  Commonly used as
    ///   the 404 handler when routes are used.
    /// </param>
    public Server(string hostname, int port, Func<HttpContext, Task> defaultRoute)
    {
      if (string.IsNullOrEmpty(hostname)) hostname = "*";
      if (port         < 1) throw new ArgumentOutOfRangeException(nameof(port));
      if (defaultRoute == null) throw new ArgumentNullException(nameof(defaultRoute));

      _HttpListener = new HttpListener();

      _ListenerHostnames = new List<string>();
      _ListenerHostnames.Add(hostname);
      _ListenerPort = port;
      _DefaultRoute = defaultRoute;
      _TokenSource = new CancellationTokenSource();
      _Token = _TokenSource.Token;

      Task.Run(() => StartServer(_Token), _Token);
    }

    /// <summary>
    ///   Creates a new instance of the Watson Webserver.
    /// </summary>
    /// <param name="hostnames">
    ///   Hostnames or IP addresses on which to listen.  Note: multiple listener endpoints is not
    ///   supported on all platforms.
    /// </param>
    /// <param name="port">TCP port on which to listen.</param>
    /// <param name="defaultRoute">
    ///   Method used when a request is received and no matching routes are found.  Commonly used as
    ///   the 404 handler when routes are used.
    /// </param>
    public Server(List<string> hostnames, int port, Func<HttpContext, Task> defaultRoute)
    {
      if (port         < 1) throw new ArgumentOutOfRangeException(nameof(port));
      if (defaultRoute == null) throw new ArgumentNullException(nameof(defaultRoute));

      _HttpListener = new HttpListener();

      _ListenerHostnames = new List<string>();
      if (hostnames == null || hostnames.Count < 1)
        _ListenerHostnames.Add("*");
      else
        foreach (var curr in hostnames)
          _ListenerHostnames.Add(curr);

      _ListenerPort = port;
      _DefaultRoute = defaultRoute;
      _TokenSource = new CancellationTokenSource();
      _Token = _TokenSource.Token;

      Task.Run(() => StartServer(_Token), _Token);
    }

    #endregion

    #region Private-Methods

    /// <summary>
    ///   Tear down the server and dispose of background workers.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
      if (!disposing)
        return;

      if (_HttpListener != null)
      {
        if (_HttpListener.IsListening) _HttpListener.Stop();
        _HttpListener.Close();
      }

      _TokenSource.Cancel();
    }

    private void StartServer(CancellationToken token)
    {
      Task.Run(() => AcceptConnections(token), token);
      _Terminator.WaitOne();
    }

    private void AcceptConnections(CancellationToken token)
    {
      try
      {
        #region Start-Listeners

        foreach (var curr in _ListenerHostnames)
          _HttpListener.Prefixes.Add("http://" + curr + ":" + _ListenerPort + "/");

        _HttpListener.Start();

        #endregion

        while (_HttpListener.IsListening)
          ThreadPool.QueueUserWorkItem(c =>
          {
            if (token.IsCancellationRequested) throw new OperationCanceledException();

            var listenerContext = c as HttpListenerContext;
            HttpContext ctx = null;

            try
            {
              ctx = new HttpContext(listenerContext);

              #region Process-Via-Routing

              Task.Run(() =>
              {
                Func<HttpContext, Task> handler = null;

                handler = _endpoints.Match(ctx.Request.Method, ctx.Request.RawUrlWithoutQuery);
                if (handler != null)
                {
                  handler(ctx).RunSynchronously();
                  return;
                }

                _DefaultRoute(ctx).RunSynchronously();
              });

              #endregion
            }
            catch
            {
              // ignore
            }
          }, _HttpListener.GetContext());
      }
      catch
      {
        // ignore
      }
    }

    #endregion
  }
}