using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tfres
{
  public class Server : IDisposable
  {
    #region Constructor

    /// <summary>
    ///   Creates a new instance of the Watson Webserver.
    /// </summary>
    /// <param name="ip">IP address on which to listen.</param>
    /// <param name="port">TCP port on which to listen.</param>
    /// <param name="defaultRequestHandler">
    ///   Method used when a request is received and no routes are defined.  Commonly used as
    ///   the 404 handler when routes are used.
    /// </param>
    public Server(string ip, int port, Func<HttpRequest, HttpResponse> defaultRequestHandler)
    {
      if (string.IsNullOrEmpty(ip)) ip = "*";
      if (port < 1) throw new ArgumentOutOfRangeException(nameof(port));

      _http = new HttpListener();
      _listenerPrefix = $"http://{ip}:{port}/";

      _endpointManager = new EndpointManager();
      _defaultRoute = defaultRequestHandler;

      _tokenSource = new CancellationTokenSource();
      var token = _tokenSource.Token;
      Task.Run(() => StartServer(token), token);
    }

    #endregion

    #region Public-Members

    /// <summary>
    ///   Indicates whether or not the server is listening.
    /// </summary>
    public bool IsListening => _http != null ? _http.IsListening : false;

    #endregion

    #region Private-Members

    private readonly EventWaitHandle _terminator =
      new EventWaitHandle(false, EventResetMode.ManualReset, "UserIntervention");

    private readonly HttpListener _http;
    private readonly string _listenerPrefix;

    private readonly EndpointManager _endpointManager;
    private readonly Func<HttpRequest, HttpResponse> _defaultRoute;

    private readonly CancellationTokenSource _tokenSource;

    #endregion

    #region Public-Methods

    /// <summary>
    ///   Tear down the server and dispose of background workers.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
    }

    /// <summary>
    ///   Add a static route to the server.
    /// </summary>
    /// <param name="verb">The HTTP method, i.e. GET, PUT, POST, DELETE.</param>
    /// <param name="path">The raw URL to match, i.e. /foo/bar.</param>
    /// <param name="handler">The method to which control should be passed.</param>
    public void AddEndpoint(HttpVerb verb, string path, Func<HttpRequest, HttpResponse> handler)
    {
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
      if (handler == null) throw new ArgumentNullException(nameof(handler));

      _endpointManager.Add(verb, path, handler);
    }

    #endregion

    #region Private-Methods

    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (_http != null)
        {
          if (_http.IsListening) _http.Stop();
          _http.Close();
        }

        _tokenSource.Cancel();
      }
    }

    private void StartServer(CancellationToken token)
    {
      Task.Run(() => AcceptConnections(token), token);
      _terminator.WaitOne();
    }

    private void AcceptConnections(CancellationToken token)
    {
      try
      {
        _http.Prefixes.Add(_listenerPrefix);
        _http.Start();

        while (_http.IsListening)
          ThreadPool.QueueUserWorkItem(c =>
          {
            if (token.IsCancellationRequested) throw new OperationCanceledException();

            var context = c as HttpListenerContext;

            try
            {
              #region Populate-Http-Request-Object

              var currRequest = new HttpRequest(context);

              #endregion

              #region Process-OPTIONS-Request

              if (currRequest.Method == "OPTIONS")
              {
                OptionsProcessor(context, currRequest);
                return;
              }

              #endregion

              #region Send-to-Handler

              Task.Run(() =>
              {
                #region Find-Route

                Enum.TryParse(currRequest.Method, out HttpVerb verb);
                var handler = _endpointManager.Match(verb, currRequest.RawUrlWithoutQuery);
                var currResponse = handler != null ? handler(currRequest) : DefaultRouteProcessor(context, currRequest);

                #endregion

                #region Return

                if (currResponse == null)
                {
                  SendResponse(
                               context,
                               currRequest,
                               BuildErrorResponse(500, "Unable to generate response", null),
                               TfresCommon.AddToDict("content-type", "application/json", null),
                               500);
                  return;
                }

                var headers = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(currResponse.ContentType))
                  headers.Add("content-type", currResponse.ContentType);

                if (currResponse.Headers != null && currResponse.Headers.Count > 0)
                  foreach (var curr in currResponse.Headers)
                    headers = TfresCommon.AddToDict(curr.Key, curr.Value, headers);

                SendResponse(
                             context,
                             currRequest,
                             currResponse.Data,
                             headers,
                             currResponse.StatusCode);

                #endregion
              });

              #endregion
            }
            catch (Exception)
            {
              // ignore
            }
          }, _http.GetContext());
      }
      catch (Exception)
      {
        // ignore
      }
    }

    private HttpResponse DefaultRouteProcessor(HttpListenerContext context, HttpRequest request)
    {
      if (context == null) throw new ArgumentNullException(nameof(context));
      if (request == null) throw new ArgumentNullException(nameof(request));
      var ret = _defaultRoute(request);
      if (ret != null) return ret;
      ret = new HttpResponse(request, false, 500, null, "application/json", "Unable to generate response");
      return ret;
    }

    private byte[] BuildErrorResponse(
      int status,
      string text,
      byte[] data)
    {
      var ret = new Dictionary<string, object> {{"data", data}, {"success", false}, {"http_status", status}};

      switch (status)
      {
        case 200:
          ret.Add("http_text", "OK");
          break;

        case 201:
          ret.Add("http_text", "Created");
          break;

        case 301:
          ret.Add("http_text", "Moved Permanently");
          break;

        case 302:
          ret.Add("http_text", "Moved Temporarily");
          break;

        case 304:
          ret.Add("http_text", "Not Modified");
          break;

        case 400:
          ret.Add("http_text", "Bad Request");
          break;

        case 401:
          ret.Add("http_text", "Unauthorized");
          break;

        case 403:
          ret.Add("http_text", "Forbidden");
          break;

        case 404:
          ret.Add("http_text", "Not Found");
          break;

        case 405:
          ret.Add("http_text", "Method Not Allowed");
          break;

        case 429:
          ret.Add("http_text", "Too Many Requests");
          break;

        case 500:
          ret.Add("http_text", "Internal Server Error");
          break;

        case 501:
          ret.Add("http_text", "Not Implemented");
          break;

        case 503:
          ret.Add("http_text", "Service Unavailable");
          break;

        default:
          ret.Add("http_text", "Unknown Status");
          break;
      }

      ret.Add("text", text);
      return Encoding.UTF8.GetBytes(TfresCommon.SerializeJson(ret));
    }

    private void SendResponse(
      HttpListenerContext context,
      HttpRequest req,
      object data,
      Dictionary<string, string> headers,
      int status)
    {
      var responseLen = 0;
      HttpListenerResponse response = null;

      try
      {
        #region Set-Variables

        if (data != null)
        {
          switch (data)
          {
            case string _:
            {
              if (!string.IsNullOrEmpty(data.ToString())) responseLen = data.ToString().Length;
              break;
            }
            case byte[] _:
            {
              if (((byte[]) data).Length > 0)
                responseLen = ((byte[]) data).Length;
              break;
            }
            default:
              responseLen = TfresCommon.SerializeJson(data).Length;
              break;
          }
        }

        #endregion

        #region Status-Code-and-Description

        response = context.Response;
        response.StatusCode = status;

        switch (status)
        {
          case 200:
            response.StatusDescription = "OK";
            break;

          case 201:
            response.StatusDescription = "Created";
            break;

          case 301:
            response.StatusDescription = "Moved Permanently";
            break;

          case 302:
            response.StatusDescription = "Moved Temporarily";
            break;

          case 304:
            response.StatusDescription = "Not Modified";
            break;

          case 400:
            response.StatusDescription = "Bad Request";
            break;

          case 401:
            response.StatusDescription = "Unauthorized";
            break;

          case 403:
            response.StatusDescription = "Forbidden";
            break;

          case 404:
            response.StatusDescription = "Not Found";
            break;

          case 405:
            response.StatusDescription = "Method Not Allowed";
            break;

          case 429:
            response.StatusDescription = "Too Many Requests";
            break;

          case 500:
            response.StatusDescription = "Internal Server Error";
            break;

          case 501:
            response.StatusDescription = "Not Implemented";
            break;

          case 503:
            response.StatusDescription = "Service Unavailable";
            break;

          default:
            response.StatusDescription = "Unknown Status";
            break;
        }

        #endregion

        #region Response-Headers

        response.AddHeader("Access-Control-Allow-Origin", "*");
        response.ContentType = req.ContentType;

        var headerCount = 0;

        if (headers != null)
          if (headers.Count > 0)
            headerCount = headers.Count;

        if (headerCount > 0 && headers != null)
          foreach (var curr in headers)
            response.AddHeader(curr.Key, curr.Value);

        #endregion

        #region Handle-HEAD-Request

        if (string.CompareOrdinal(req.Method.ToLower(), "head") == 0) data = null;

        #endregion

        #region Send-Response

        var output = response.OutputStream;

        try
        {
          if (data != null)
          {
            #region Response-Body-Attached

            if (data is string)
            {
              #region string

              if (!string.IsNullOrEmpty(data.ToString()))
                if (data.ToString().Length > 0)
                {
                  var buffer = Encoding.UTF8.GetBytes(data.ToString());
                  response.ContentLength64 = buffer.Length;
                  output.Write(buffer, 0, buffer.Length);
                  output.Close();
                }

              #endregion
            }
            else if (data is byte[])
            {
              #region byte-array

              response.ContentLength64 = responseLen;
              output.Write((byte[]) data, 0, responseLen);
              output.Close();

              #endregion
            }
            else
            {
              #region other

              response.ContentLength64 = responseLen;
              output.Write(Encoding.UTF8.GetBytes(TfresCommon.SerializeJson(data)), 0, responseLen);
              output.Close();

              #endregion
            }

            #endregion
          }
          else
          {
            #region No-Response-Body

            response.ContentLength64 = 0;
            output.Flush();
            output.Close();

            #endregion
          }
        }
        catch (HttpListenerException)
        {
          // ignore
        }
        finally
        {
          response.Close();
        }

        #endregion
      }
      catch (IOException)
      {
        // ignore
      }
      catch (HttpListenerException)
      {
        // ignore
      }
      catch (Exception)
      {
        // ignore
      }
      finally
      {
        response?.Close();
      }
    }

    private void OptionsProcessor(HttpListenerContext context, HttpRequest req)
    {
      var response = context.Response;
      response.StatusCode = 200;

      string[] requestedHeaders = null;
      if (req.Headers != null)
        foreach (var curr in req.Headers)
        {
          if (string.IsNullOrEmpty(curr.Key)) continue;
          if (string.IsNullOrEmpty(curr.Value)) continue;
          if (string.CompareOrdinal(curr.Key.ToLower(), "access-control-request-headers") == 0)
          {
            requestedHeaders = curr.Value.Split(',');
            break;
          }
        }

      var headers = "";

      if (requestedHeaders != null)
      {
        var addedCount = 0;
        foreach (var curr in requestedHeaders)
        {
          if (string.IsNullOrEmpty(curr)) continue;
          if (addedCount > 0) headers += ", ";
          headers += ", " + curr;
          addedCount++;
        }
      }

      response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, PATCH, DELETE, COPY, HEAD, OPTIONS, LINK, UNLINK, PURGE, LOCK, UNLOCK, PROPFIND, VIEW");
      response.AddHeader("Access-Control-Allow-Headers", "*, Content-Type, X-Requested-With, " + headers);
      response.AddHeader("Access-Control-Expose-Headers", "Content-Type, X-Requested-With, "   + headers);
      response.AddHeader("Access-Control-Allow-Origin", "*");
      response.AddHeader("Accept", "*/*");
      response.AddHeader("Accept-Language", "en-US, en");
      response.AddHeader("Accept-Charset", "ISO-8859-1, utf-8");
      response.AddHeader("Connection", "keep-alive");
      response.AddHeader("Host", _listenerPrefix);
      response.ContentLength64 = 0;
      response.Close();
    }

    #endregion
  }
}