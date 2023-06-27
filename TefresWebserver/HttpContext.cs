#region

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
    public async Task<WebSocket> GetWebSocket()
    {
      if (!_ctx.Request.IsWebSocketRequest)
        return null;

      var task = await _ctx.AcceptWebSocketAsync(subProtocol: null);
      await HandleWebSocketConnection(task.WebSocket);

      return task.WebSocket;
    }

    private async Task HandleWebSocketConnection(WebSocket webSocket)
    {
      var buffer = new byte[1024];

      while (webSocket.State == WebSocketState.Open)
      {
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken);
        if (result.MessageType == WebSocketMessageType.Close)
        {
          await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken);
          break;
        }
        else
        {
          // Process the received message
          var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
          Console.WriteLine($"Received message: {message}");

          // Send updates to the client
          var responseMessage = $"Server update: {DateTime.Now}";
          var responseBytes = Encoding.UTF8.GetBytes(responseMessage);
          await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken);
        }
      }
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