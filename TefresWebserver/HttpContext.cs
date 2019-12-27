#region

using System;
using System.Collections.Generic;
using System.Net;

#endregion

namespace Tfres
{
  /// <summary>
  ///   HTTP context including both request and response.
  /// </summary>
  public class HttpContext
  {
    private int _streamBufferSize = 65536;

    /// <summary>
    ///   The HTTP request that was received.
    /// </summary>
    public HttpRequest Request { get; set; }

    /// <summary>
    ///   The HTTP response that will be sent.  This object is preconstructed on your behalf and can be modified directly.
    /// </summary>
    public HttpResponse Response { get; set; }

    private HttpContext()
    {
    }

    internal HttpContext(HttpListenerContext ctx)
    {
      Request = new HttpRequest(ctx);
      Response = new HttpResponse(Request, ctx ?? throw new ArgumentNullException(nameof(ctx)), _streamBufferSize);
    }

    /// <summary>
    ///   Buffer size to use while writing the response from a supplied stream.
    /// </summary>
    public int StreamBufferSize
    {
      get => _streamBufferSize;
      set
      {
        if (value < 1) throw new ArgumentException("StreamBufferSize must be greater than zero bytes.");
        _streamBufferSize = value;
      }
    }

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

    public string PostDataAsString => Request.PostDataAsString;
  }
}