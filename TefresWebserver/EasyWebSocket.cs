using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tfres
{
  public class EasyWebSocket
  {
    private WebSocket _webSocket;
    private int _bufferSize;
    private Task _task;
    private CancellationToken _cancellationToken = new CancellationToken();

    private EasyWebSocket() { }

    public static EasyWebSocket Create(WebSocket webSocket, int bufferSize)
    {
      var res = new EasyWebSocket { _webSocket = webSocket, _bufferSize = bufferSize };
      res._task = Task.Run(() => res.HandleWebSocketConnection());
      return res;
    }

    public int BufferSize
    {
      get => _bufferSize;
      set => _bufferSize = value;
    }

    public event EventHandler<string> MessageReceived;

    public event EventHandler Closed;

    private void HandleWebSocketConnection()
    {
      while (_webSocket.State == WebSocketState.Open)
      {
        try
        {
          var buffer = new byte[BufferSize];
          
          var task = _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);
          task.Wait();

          var result = task.Result;

          if (result.MessageType == WebSocketMessageType.Close)
          {
            var closing = _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", _cancellationToken);
            closing.Wait();
            break;
          }
          else
          {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            MessageReceived?.Invoke(this, message);
          }
        }
        catch
        {
          // ignore
        }
      }
      
      Closed?.Invoke(this, EventArgs.Empty);
    }

    public async Task Send(string message)
    {
      try
      {
        var bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationToken);
      }
      catch
      {
        // ignore
      }
    }

    public void Wait()
    {
      // wait until socket is closed
      _task.Wait();
    }
  }
}
