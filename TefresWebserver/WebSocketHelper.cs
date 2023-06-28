using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tfres
{
  public static class WebSocketHelper
  {
    public static void Send(this Task<KeyValuePair<Guid, WebSocket>> connection, string text)
    {
      connection.Result.Value.Send(text);
    }

    public static void Send(this WebSocket socket, string text)
    {
      Send(socket, text, new CancellationToken());
    }

    public static void Send(this WebSocket socket, string text, CancellationToken cancellationToken)
    {
      socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, true, cancellationToken).Wait();
    }

    public static Task KeepOpenAndRecive(this Task<KeyValuePair<Guid, WebSocket>> connection, HttpContext context, Action<string> action, int bufferSize = 64)
    {
      return connection.Result.Value.KeepOpenAndRecive(context, action, bufferSize);
    }

    public static async Task KeepOpenAndRecive(this WebSocket socket, HttpContext context, Action<string> action, int bufferSize = 64)
    {
      var buffer = new byte[bufferSize];
      using (var ms = new MemoryStream())
        while (socket.State == WebSocketState.Open && socket.CloseStatus == null && !context.CancellationToken.IsCancellationRequested)
        {
          var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), context.CancellationToken);
          if (result.MessageType == WebSocketMessageType.Close)
          {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", context.CancellationToken);
            break;
          }

          if(result.Count > 0)
            ms.Write(buffer, 0, result.Count);

          if(result.EndOfMessage)
          {
            action(Encoding.UTF8.GetString(ms.ToArray()));
            ms.Position = 0;
            ms.SetLength(0);
          }
        }
    }
  }
}
