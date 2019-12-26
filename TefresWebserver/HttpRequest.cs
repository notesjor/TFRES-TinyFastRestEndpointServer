#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;

#endregion

namespace Tfres
{
  /// <summary>
  ///   Data extracted from an incoming HTTP request.
  /// </summary>
  public class HttpRequest
  {
    #region Constructor

    /// <summary>
    ///   Construct a new HTTP request from a given HttpListenerContext.
    /// </summary>
    /// <param name="ctx">The HttpListenerContext for the request.</param>
    public HttpRequest(HttpListenerContext ctx)
    {
      #region Check-for-Null-Values

      if (ctx         == null) throw new ArgumentNullException(nameof(ctx));
      if (ctx.Request == null) throw new ArgumentNullException(nameof(ctx.Request));

      #endregion

      #region Parse-Variables

      var position = 0;
      var inQuery = 0;
      var tempString = "";
      var queryString = "";

      #endregion

      #region Standard-Request-Items

      TimestampUtc = DateTime.Now.ToUniversalTime();
      _protocolVersion = "HTTP/" + ctx.Request.ProtocolVersion;
      SourceIp = ctx.Request?.RemoteEndPoint?.Address?.ToString();
      SourcePort = ctx.Request?.RemoteEndPoint?.Port ?? 0;
      DestIp = ctx.Request?.LocalEndPoint?.Address?.ToString();
      DestPort = ctx.Request?.LocalEndPoint?.Port ?? 0;
      Method = ctx.Request.HttpMethod;
      _fullUrl = string.Copy(ctx.Request.Url.ToString().Trim());
      RawUrlWithoutQuery = string.Copy(ctx.Request.RawUrl.Trim());
      _keepalive = ctx.Request.KeepAlive;
      var contentLength = ctx.Request.ContentLength64;
      _useragent = ctx.Request.UserAgent;
      ContentType = ctx.Request.ContentType;

      Headers = new Dictionary<string, string>();

      #endregion

      #region Raw-URL-and-Querystring

      if (!string.IsNullOrEmpty(RawUrlWithoutQuery))
      {
        #region Process-Raw-URL-and-Populate-Raw-URL-Elements

        foreach (var c in RawUrlWithoutQuery)
        {
          if (inQuery == 1)
          {
            queryString += c;
            continue;
          }

          if (position                              == 0 &&
              string.CompareOrdinal(tempString, "") == 0 &&
              c                                     == '/')
            continue;

          if (c != '/' && c != '?') tempString += c;

          if (c == '/' || c == '?')
          {
            position++;
            tempString = "";
          }

          if (c == '?') inQuery = 1;
        }

        #endregion

        #region Populate-Querystring

        GetDataAsString = queryString.Length > 0 ? queryString : null;

        #endregion
      }

      #endregion

      #region Remove-Querystring-from-Raw-URL

      if (RawUrlWithoutQuery.Contains("?"))
        RawUrlWithoutQuery = RawUrlWithoutQuery.Substring(0, RawUrlWithoutQuery.IndexOf("?", StringComparison.Ordinal));

      #endregion

      #region Check-for-Full-URL

      try
      {
        var uri = new Uri(_fullUrl);
        _destHostname = uri.Host;
        _destHostPort = uri.Port;
      }
      catch (Exception)
      {
        // ignore
      }

      #endregion

      #region Headers

      Headers = new Dictionary<string, string>();
      for (var i = 0; i < ctx.Request.Headers.Count; i++)
      {
        var key = string.Copy(ctx.Request.Headers.GetKey(i));
        var val = string.Copy(ctx.Request.Headers.Get(i));
        Headers = TfresCommon.AddToDict(key, val, Headers);
      }

      #endregion

      #region Copy-Payload

      if (contentLength > 0)
        if (string.CompareOrdinal(Method.ToLower().Trim(), "get") != 0)
          try
          {
            if (contentLength < 1)
            {
              PostDataAsByteArray = null;
            }
            else
            {
              PostDataAsByteArray = new byte[contentLength];
              var bodyStream = ctx.Request.InputStream;

              PostDataAsByteArray = TfresCommon.StreamToBytes(bodyStream);
            }
          }
          catch (Exception)
          {
            PostDataAsByteArray = null;
          }

      #endregion
    }

    #endregion

    #region Public-Members

    /// <summary>
    ///   UTC timestamp from when the request was received.
    /// </summary>
    public DateTime TimestampUtc { get; }

    /// <summary>
    ///   The protocol and version.
    /// </summary>
    private readonly string _protocolVersion;

    /// <summary>
    ///   IP address of the requestor (client).
    /// </summary>
    public string SourceIp { get; }

    /// <summary>
    ///   TCP port from which the request originated on the requestor (client).
    /// </summary>
    public int SourcePort { get; }

    /// <summary>
    ///   IP address of the recipient (server).
    /// </summary>
    public string DestIp { get; }

    /// <summary>
    ///   TCP port on which the request was received by the recipient (server).
    /// </summary>
    public int DestPort { get; }

    /// <summary>
    ///   The destination hostname as found in the request line, if present.
    /// </summary>
    private readonly string _destHostname;

    /// <summary>
    ///   The destination host port as found in the request line, if present.
    /// </summary>
    private readonly int _destHostPort;

    /// <summary>
    ///   Specifies whether or not the client requested HTTP keepalives.
    /// </summary>
    private readonly bool _keepalive;

    /// <summary>
    ///   The HTTP verb used in the request.
    /// </summary>
    public string Method { get; }

    /// <summary>
    ///   The full URL as sent by the requestor (client).
    /// </summary>
    private readonly string _fullUrl;

    /// <summary>
    ///   The raw (relative) URL without the querystring attached.
    /// </summary>
    public string RawUrlWithoutQuery { get; }

    /// <summary>
    ///   The useragent specified in the request.
    /// </summary>
    private readonly string _useragent;

    /// <summary>
    ///   The content type as specified by the requestor (client).
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    ///   The headers found in the request.
    /// </summary>
    public Dictionary<string, string> Headers { get; }

    #endregion

    #region Public-Internal-Classes

    #endregion

    #region Private-Internal-Classes

    #endregion

    #region Public-Methods

    /// <summary>
    ///   Retrieve a string-formatted, human-readable copy of the HttpRequest instance.
    /// </summary>
    /// <returns>String-formatted, human-readable copy of the HttpRequest instance.</returns>
    public override string ToString()
    {
      var ret = "";
      var contentLength = 0;
      if (PostDataAsByteArray != null) contentLength = PostDataAsByteArray.Length;

      ret += "--- HTTP Request ---" + Environment.NewLine;
      ret += TimestampUtc.ToString("MM/dd/yyyy HH:mm:ss") + " " + SourceIp + ":" + SourcePort + " to " + DestIp + ":" +
             DestPort                                     + Environment.NewLine;
      ret += "  " + Method + " " + RawUrlWithoutQuery + " " + _protocolVersion   + Environment.NewLine;
      ret += "  Full URL    : " + _fullUrl                                       + Environment.NewLine;
      ret += "  Raw URL     : " + RawUrlWithoutQuery                             + Environment.NewLine;
      ret += "  Querystring : " + GetDataAsString                                + Environment.NewLine;
      ret += "  Useragent   : " + _useragent + " (Keepalive " + _keepalive + ")" + Environment.NewLine;
      ret += "  Content     : " + ContentType + " (" + contentLength + " bytes)" + Environment.NewLine;
      ret += "  Destination : " + _destHostname + ":" + _destHostPort            + Environment.NewLine;

      if (Headers != null && Headers.Count > 0)
      {
        ret += "  Headers     : " + Environment.NewLine;
        foreach (var curr in Headers) ret += "    " + curr.Key + ": " + curr.Value + Environment.NewLine;
      }
      else
      {
        ret += "  Headers     : none" + Environment.NewLine;
      }

      if (PostDataAsByteArray != null)
      {
        ret += "  Data        : "                           + Environment.NewLine;
        ret += Encoding.UTF8.GetString(PostDataAsByteArray) + Environment.NewLine;
      }
      else
      {
        ret += "  Data        : [null]" + Environment.NewLine;
      }

      return ret;
    }

    /// <summary>
    ///   Return Post-Data as T
    /// </summary>
    /// <returns>Post-Data as T</returns>
    public T PostData<T>()
    {
      return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(PostDataAsByteArray));
    }

    /// <summary>
    ///   The request body as sent by the requestor (client).
    /// </summary>
    public byte[] PostDataAsByteArray { get; }

    public string PostDataAsString => Encoding.UTF8.GetString(PostDataAsByteArray);

    /// <summary>
    ///   Return Data send as GET-Parameter
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, string> GetData(bool keyToLowercase = true)
    {
      try
      {
        if (string.IsNullOrEmpty(GetDataAsString))
          return new Dictionary<string, string>();

        var split = GetDataAsString.Split(new[] {"&"}, StringSplitOptions.RemoveEmptyEntries);
        var res = new Dictionary<string, string>();
        foreach (var x in split)
          try
          {
            var entry = x.Split(new[] {"="}, StringSplitOptions.RemoveEmptyEntries);
            if (entry.Length != 2)
              continue;

            var key = HttpUtility.UrlDecode(entry[0]);
            if (keyToLowercase)
              key = key.ToLower();

            if (res.ContainsKey(key))
              res[key] = HttpUtility.UrlDecode(entry[1]);
            else
              res.Add(key, HttpUtility.UrlDecode(entry[1]));
          }
          catch
          {
            // ignore
          }

        return res;
      }
      catch
      {
        return new Dictionary<string, string>();
      }
    }

    /// <summary>
    ///   The querystring attached to the URL.
    /// </summary>
    public string GetDataAsString { get; }

    #endregion
  }
}