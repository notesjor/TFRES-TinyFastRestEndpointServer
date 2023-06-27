﻿using System;
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

      while (socket.State == WebSocketState.Open)
      {
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), context.CancellationToken);
        if (result.MessageType == WebSocketMessageType.Close)
        {
          await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", context.CancellationToken);
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
          await socket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, context.CancellationToken);
        }
      }
    }
  }
}
