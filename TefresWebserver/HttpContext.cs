﻿#region

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
      Response = new HttpResponse(Request, ctx ?? throw new ArgumentNullException(nameof(ctx)));
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