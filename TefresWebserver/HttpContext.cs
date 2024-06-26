﻿#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

#endregion

namespace Tfres
{
  /// <summary>
  ///   HTTP context including both request and response.
  /// </summary>
  public class HttpContext
  {
    private Dictionary<Guid, WebSocket> _sockets;
    private HttpListenerContext _ctx;

    private HttpContext()
    {
    }

    internal HttpContext(HttpListenerContext ctx, JsonSerializer serializer, CancellationToken token, ref Dictionary<Guid, WebSocket> sockets)
    {
      _ctx = ctx;
      _sockets = sockets;      

      Request = new HttpRequest(ctx);
      Response = new HttpResponse(Request, ctx ?? throw new ArgumentNullException(nameof(ctx)), serializer);
      CancellationToken = token;
    }

    /// <summary>
    /// Return the WebSocket
    /// </summary>
    public async Task<KeyValuePair<Guid, WebSocket>> GetWebSocket()
    {
      if (!_ctx.Request.IsWebSocketRequest)
        return new KeyValuePair<Guid, WebSocket>(Guid.Empty, null);

      var task = await _ctx.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
      //await HandleWebSocketConnection(task.WebSocket).ConfigureAwait(false);

      var guid = Guid.NewGuid();
      var res = task.WebSocket;
      _sockets.Add(guid, res);

      return new KeyValuePair<Guid, WebSocket>(guid, res);
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