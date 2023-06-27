#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
    public bool IsListening => _httpListener?.IsListening ?? false;

    /// <summary>
    ///   Indicate the buffer size to use when reading from a stream to send data to a requestor.
    /// </summary>
    public int StreamReadBufferSize
    {
      get => _streamReadBufferSize;
      set
      {
        if (value < 1) throw new ArgumentException("StreamReadBufferSize must be greater than zero.");
        _streamReadBufferSize = value;
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
    public void AddEndpoint(HttpMethod verb, string path, Action<HttpContext> handler)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
      if (handler == null) throw new ArgumentNullException(nameof(handler));

      _endpoints.Add(verb, path, handler);
    }

    /// <summary>
    ///   Set a Timeout in seconds.
    /// </summary>
    public int Timeout { get; set; } = 0;

    /// <summary>
    /// Default Serializer
    /// </summary>
    public JsonSerializer Serializer { get; set; }

    #endregion

    #region Private-Members

    private readonly HttpListener _httpListener;
    private readonly List<string> _listenerHostnames;
    private readonly int _listenerPort;
    private int _streamReadBufferSize = 65536;

    private readonly Action<HttpContext> _defaultRoute;

    private readonly CancellationTokenSource _tokenSource;
    private readonly CancellationToken _token;

    #endregion

    #region Constructor

    /// <summary>
    ///   Creates a new instance of the Watson Webserver.
    /// </summary>
    /// <param name="hostname">Hostname or IP address on which to listen.</param>
    /// <param name="port">TCP port on which to listen.</param>
    /// <param name="continueWith">If the Task ends, you can perform optional actions</param>
    /// <param name="defaultRoute">
    ///   Method used when a request is received and no matching routes are found.  Commonly used as
    ///   the 404 handler when routes are used.
    /// </param>
    public Server(string hostname, int port, Action<HttpContext> defaultRoute, Action<Task> continueWith = null)
    {
      if (string.IsNullOrEmpty(hostname))
        hostname = "*";
      if (port < 1 || port > 65535)
        throw new ArgumentOutOfRangeException(nameof(port));
      
      Serializer = new JsonSerializer();
      _httpListener = new HttpListener();

      _listenerHostnames = new List<string> { hostname };
      _listenerPort = port;
      _defaultRoute = defaultRoute ?? throw new ArgumentNullException(nameof(defaultRoute));
      _tokenSource = new CancellationTokenSource();

      StartServer(_tokenSource.Token, continueWith);      
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
    /// <param name="continueWith">If the Task ends, you can perform optional actions</param>
    public Server(List<string> hostnames, int port, Action<HttpContext> defaultRoute, Action<Task> continueWith = null)
    {
      if (port < 1) throw new ArgumentOutOfRangeException(nameof(port));
      if (defaultRoute == null) throw new ArgumentNullException(nameof(defaultRoute));

      _httpListener = new HttpListener();

      _listenerHostnames = new List<string>();
      if (hostnames == null || hostnames.Count < 1)
        _listenerHostnames.Add("*");
      else
        foreach (var curr in hostnames)
          _listenerHostnames.Add(curr);

      _listenerPort = port;
      _defaultRoute = defaultRoute;
      _tokenSource = new CancellationTokenSource();

      StartServer(_token, continueWith);
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

      foreach(var curr in _sockets)
        curr.Value.Dispose();
      _sockets.Clear();

      if (_httpListener != null)
      {
        if (_httpListener.IsListening) _httpListener.Stop();
        _httpListener.Close();
      }

      _tokenSource.Cancel();
    }

    private void StartServer(CancellationToken token, Action<Task> continueWith)
    {
      var task = Task.Run(() => AcceptConnections(token), token);
      if (continueWith != null)
        task.ContinueWith(continueWith);  
    }

    private Dictionary<Guid, WebSocket> _sockets = new Dictionary<Guid, WebSocket>();

    private void AcceptConnections(CancellationToken token)
    {
      try
      {
        #region Start-Listeners

        foreach (var curr in _listenerHostnames)
          _httpListener.Prefixes.Add("http://" + curr + ":" + _listenerPort + "/");

        _httpListener.Start();

        #endregion

        while (_httpListener.IsListening && !token.IsCancellationRequested)
          ThreadPool.QueueUserWorkItem(c =>
          {
            if (token.IsCancellationRequested)
              return;

            var listenerContext = c as HttpListenerContext;

            try
            {
              var ctx = new HttpContext(listenerContext, Serializer, token, ref _sockets);

              try
              {                
                var handler = _endpoints.Match(ctx.Request.Verb, ctx.Request.RawUrlWithoutQuery) ?? _defaultRoute;
                var task =
                  (new Func<HttpContext, Task>(async (p) => await Task.Run(() => handler(p), token))).Invoke(ctx);

                if (Timeout > 0)
                  task.Wait(Timeout * 1000, token);
                else
                  task.Wait(token);

                ctx.Request.Close();
                ctx.Response.Close();
              }
              catch
              {
                ctx.Response.Send(HttpStatusCode.InternalServerError, "");
              }
            }
            catch
            {
              // ignore
            }
          }, _httpListener.GetContext());

        _httpListener.Stop();
      }
      catch
      {
        // ignore
      }
    }

    #endregion

    #region Public-Methods
    public void Cancel()
    {
      _tokenSource.Cancel();
    }

    public void SendToAll(string socketMessage)
    {
      foreach(var socket in _sockets)
        try
        {
          socket.Value.Send(socketMessage);
        }
        catch
        {
          // ignore
        }
    }

    public void SendTo(Guid guid, string socketMessage)
    {
      if(!_sockets.ContainsKey(guid))
        throw new ArgumentException("Guid not found", nameof(guid));
      _sockets[guid].Send(socketMessage);
    }
    #endregion
  }
}