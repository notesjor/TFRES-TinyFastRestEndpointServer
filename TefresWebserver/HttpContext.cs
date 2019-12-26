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
    private readonly HttpListenerContext _Context;

    private int _StreamBufferSize = 65536;

    /// <summary>
    ///   The HTTP request that was received.
    /// </summary>
    public HttpRequest Request;

    /// <summary>
    ///   The HTTP response that will be sent.  This object is preconstructed on your behalf and can be modified directly.
    /// </summary>
    public HttpResponse Response;

    private HttpContext()
    {
    }

    internal HttpContext(HttpListenerContext ctx)
    {
      _Context = ctx ?? throw new ArgumentNullException(nameof(ctx));

      Request = new HttpRequest(ctx);
      Response = new HttpResponse(Request, _Context, _StreamBufferSize);
    }

    /// <summary>
    ///   Buffer size to use while writing the response from a supplied stream.
    /// </summary>
    public int StreamBufferSize
    {
      get => _StreamBufferSize;
      set
      {
        if (value < 1) throw new ArgumentException("StreamBufferSize must be greater than zero bytes.");
        _StreamBufferSize = value;
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