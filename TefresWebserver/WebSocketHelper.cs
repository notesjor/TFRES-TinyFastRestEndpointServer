using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tfres
{
  public static class WebSocketHelper
  {
    public static void Send(this WebSocket socket, string text)
    {
      Send(socket, text, new CancellationToken());
    }

    public static void Send(this WebSocket socket, string text, CancellationToken cancellationToken)
    {
      socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, true, cancellationToken).Wait();
    }
  }
}
