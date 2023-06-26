#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using Newtonsoft.Json;

#endregion

namespace Tfres
{
  /// <summary>
  ///   HTTP context including both request and response.
  /// </summary>
  public class HttpContext
  {
    private HttpListenerContext _ctx;

    private HttpContext()
    {
    }

    internal HttpContext(HttpListenerContext ctx, JsonSerializer serializer, CancellationToken token)
    {
      _ctx = ctx;

      Request = new HttpRequest(ctx);
      Response = new HttpResponse(Request, ctx ?? throw new ArgumentNullException(nameof(ctx)), serializer);
      CancellationToken = token;
    }

    /// <summary>
    /// Return the WebSocket
    /// </summary>
    public WebSocket WebSocket
    {
      get
      {
        if(!_ctx.Request.IsWebSocketRequest)
          return null;

        var task = _ctx.AcceptWebSocketAsync(null);
        task.Wait();
        return task.Result.WebSocket;
      }
    }

    /// <summary>
    /// Return the EasyWebSocket
    /// </summary>
    public EasyWebSocket WebSocketEasy(int bufferSize = 1024)
    {
      var socket = WebSocket;
      if(socket == null)
        return null;

      return EasyWebSocket.Create(socket, bufferSize);
    }

    /// <summary>
    /// Get the Post-Data as String
    /// </summary>
    public string PostDataAsString => Request.PostDataAsString;    

    /// <summary>
    ///   The HTTP request that was received.
    /// </summary>
    public HttpRequest Request { get; set; }

    /// <summary>
    ///   The HTTP response that will be sent.  This object is preconstructed on your behalf and can be modified directly.
    /// </summary>
    public HttpResponse Response { get; set; }

    /// <summary>
    ///   Return Data send as GET-Parameter
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, string> GetData(bool keyToLowercase = true) => Request.GetData();

    /// <summary>
    ///   Return Post-Data as T
    /// </summary>
    /// <returns>Post-Data as T</returns>
    public T PostData<T>() => Request.PostData<T>();

    /// <summary>
    /// If HttpContext is canceled by Client?
    /// </summary>
    public CancellationToken CancellationToken { get; }
  }
}