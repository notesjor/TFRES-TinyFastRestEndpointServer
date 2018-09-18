using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using Newtonsoft.Json;

namespace Tfres
{
  /// <summary>
  ///   Data extracted from an incoming HTTP request.
  /// </summary>
  public class HttpRequest
  {
    #region Public-Members

    /// <summary>
    ///   UTC timestamp from when the request was received.
    /// </summary>
    public DateTime TimestampUtc;

    /// <summary>
    ///   Thread ID on which the request exists.
    /// </summary>
    public int ThreadId;

    /// <summary>
    ///   The protocol and version.
    /// </summary>
    public string ProtocolVersion;

    /// <summary>
    ///   IP address of the requestor (client).
    /// </summary>
    public string SourceIp;

    /// <summary>
    ///   TCP port from which the request originated on the requestor (client).
    /// </summary>
    public int SourcePort;

    /// <summary>
    ///   IP address of the recipient (server).
    /// </summary>
    public string DestIp;

    /// <summary>
    ///   TCP port on which the request was received by the recipient (server).
    /// </summary>
    public int DestPort;

    /// <summary>
    ///   The destination hostname as found in the request line, if present.
    /// </summary>
    public string DestHostname;

    /// <summary>
    ///   The destination host port as found in the request line, if present.
    /// </summary>
    public int DestHostPort;

    /// <summary>
    ///   Specifies whether or not the client requested HTTP keepalives.
    /// </summary>
    public bool Keepalive;

    /// <summary>
    ///   The HTTP verb used in the request.
    /// </summary>
    public string Method;

    /// <summary>
    ///   The full URL as sent by the requestor (client).
    /// </summary>
    public string FullUrl;

    /// <summary>
    ///   The raw (relative) URL with the querystring attached.
    /// </summary>
    public string RawUrlWithQuery;

    /// <summary>
    ///   The raw (relative) URL without the querystring attached.
    /// </summary>
    public string RawUrlWithoutQuery;

    /// <summary>
    ///   List of items found in the raw URL.
    /// </summary>
    public List<string> RawUrlEntries;

    /// <summary>
    ///   The querystring attached to the URL.
    /// </summary>
    public string Querystring;

    /// <summary>
    ///   Dictionary containing key-value pairs from items found in the querystring.
    /// </summary>
    public Dictionary<string, string> QuerystringEntries;

    /// <summary>
    ///   The useragent specified in the request.
    /// </summary>
    public string Useragent;

    /// <summary>
    ///   The number of bytes in the request body.
    /// </summary>
    public long ContentLength;

    /// <summary>
    ///   The content type as specified by the requestor (client).
    /// </summary>
    public string ContentType;

    /// <summary>
    ///   The headers found in the request.
    /// </summary>
    public Dictionary<string, string> Headers;

    /// <summary>
    ///   The request body as sent by the requestor (client).
    /// </summary>
    public byte[] Data;

    /// <summary>
    ///   The original HttpListenerContext from which the HttpRequest was constructed.
    /// </summary>
    public HttpListenerContext ListenerContext;

    #endregion

    #region Private-Members

    private readonly Uri _Uri;
    private static readonly int TimeoutDataReadMs = 2000;
    private static readonly int DataReadSleepMs = 10;

    #endregion

    #region Constructor

    /// <summary>
    ///   Construct a new HTTP request.
    /// </summary>
    public HttpRequest()
    {
      QuerystringEntries = new Dictionary<string, string>();
      Headers = new Dictionary<string, string>();
    }

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

      var inKey = 0;
      var inVal = 0;
      var tempKey = "";
      var tempVal = "";

      #endregion

      #region Standard-Request-Items

      ThreadId = Thread.CurrentThread.ManagedThreadId;
      TimestampUtc = DateTime.Now.ToUniversalTime();
      ProtocolVersion = "HTTP/" + ctx.Request.ProtocolVersion;
      SourceIp = ctx.Request.RemoteEndPoint.Address.ToString();
      SourcePort = ctx.Request.RemoteEndPoint.Port;
      DestIp = ctx.Request.LocalEndPoint.Address.ToString();
      DestPort = ctx.Request.LocalEndPoint.Port;
      Method = ctx.Request.HttpMethod;
      FullUrl = string.Copy(ctx.Request.Url.ToString().Trim());
      RawUrlWithQuery = string.Copy(ctx.Request.RawUrl.Trim());
      RawUrlWithoutQuery = string.Copy(ctx.Request.RawUrl.Trim());
      Keepalive = ctx.Request.KeepAlive;
      ContentLength = ctx.Request.ContentLength64;
      Useragent = ctx.Request.UserAgent;
      ContentType = ctx.Request.ContentType;
      ListenerContext = ctx;

      RawUrlEntries = new List<string>();
      QuerystringEntries = new Dictionary<string, string>();
      Headers = new Dictionary<string, string>();

      #endregion

      #region Raw-URL-and-Querystring

      if (!string.IsNullOrEmpty(RawUrlWithoutQuery))
      {
        #region Initialize-Variables

        RawUrlEntries = new List<string>();
        QuerystringEntries = new Dictionary<string, string>();

        #endregion

        #region Process-Raw-URL-and-Populate-Raw-URL-Elements

        foreach (var c in RawUrlWithoutQuery)
        {
          if (inQuery == 1)
          {
            queryString += c;
            continue;
          }

          if (position                       == 0 &&
              string.Compare(tempString, "") == 0 &&
              c                              == '/')
            continue;

          if (c != '/' && c != '?') tempString += c;

          if (c == '/' || c == '?')
          {
            if (!string.IsNullOrEmpty(tempString)) RawUrlEntries.Add(tempString);

            position++;
            tempString = "";
          }

          if (c == '?') inQuery = 1;
        }

        if (!string.IsNullOrEmpty(tempString)) RawUrlEntries.Add(tempString);

        #endregion

        #region Populate-Querystring

        if (queryString.Length > 0) Querystring = queryString;
        else Querystring = null;

        #endregion

        #region Parse-Querystring

        if (!string.IsNullOrEmpty(Querystring))
        {
          inKey = 1;
          inVal = 0;
          position = 0;
          tempKey = "";
          tempVal = "";

          foreach (var c in Querystring)
          {
            if (inKey == 1)
            {
              if (c != '=')
              {
                tempKey += c;
              }
              else
              {
                inKey = 0;
                inVal = 1;
                continue;
              }
            }

            if (inVal == 1)
            {
              if (c != '&')
              {
                tempVal += c;
              }
              else
              {
                inKey = 1;
                inVal = 0;

                if (!string.IsNullOrEmpty(tempVal)) tempVal = Uri.EscapeUriString(tempVal);
                QuerystringEntries = TfresCommon.AddToDict(tempKey, tempVal, QuerystringEntries);

                tempKey = "";
                tempVal = "";
                position++;
              }
            }
          }

          if (inVal == 1)
          {
            if (!string.IsNullOrEmpty(tempVal)) tempVal = Uri.EscapeUriString(tempVal);
            QuerystringEntries = TfresCommon.AddToDict(tempKey, tempVal, QuerystringEntries);
          }
        }

        #endregion
      }

      #endregion

      #region Remove-Querystring-from-Raw-URL

      if (RawUrlWithoutQuery.Contains("?"))
        RawUrlWithoutQuery = RawUrlWithoutQuery.Substring(0, RawUrlWithoutQuery.IndexOf("?"));

      #endregion

      #region Check-for-Full-URL

      try
      {
        _Uri = new Uri(FullUrl);
        DestHostname = _Uri.Host;
        DestHostPort = _Uri.Port;
      }
      catch (Exception)
      {
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

      if (ContentLength > 0)
        if (string.Compare(Method.ToLower().Trim(), "get") != 0)
          try
          {
            if (ContentLength < 1)
            {
              Data = null;
            }
            else
            {
              Data = new byte[ContentLength];
              var bodyStream = ctx.Request.InputStream;

              Data = TfresCommon.StreamToBytes(bodyStream);
            }
          }
          catch (Exception)
          {
            Data = null;
          }

      #endregion
    }

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
      if (Data != null) contentLength = Data.Length;

      ret += "--- HTTP Request ---" + Environment.NewLine;
      ret += TimestampUtc.ToString("MM/dd/yyyy HH:mm:ss") + " " + SourceIp + ":" + SourcePort + " to " + DestIp + ":" +
             DestPort                                     + Environment.NewLine;
      ret += "  " + Method + " " + RawUrlWithoutQuery + " " + ProtocolVersion    + Environment.NewLine;
      ret += "  Full URL    : " + FullUrl                                        + Environment.NewLine;
      ret += "  Raw URL     : " + RawUrlWithoutQuery                             + Environment.NewLine;
      ret += "  Querystring : " + Querystring                                    + Environment.NewLine;
      ret += "  Useragent   : " + Useragent + " (Keepalive " + Keepalive + ")"   + Environment.NewLine;
      ret += "  Content     : " + ContentType + " (" + contentLength + " bytes)" + Environment.NewLine;
      ret += "  Destination : " + DestHostname + ":" + DestHostPort              + Environment.NewLine;

      if (Headers != null && Headers.Count > 0)
      {
        ret += "  Headers     : " + Environment.NewLine;
        foreach (var curr in Headers) ret += "    " + curr.Key + ": " + curr.Value + Environment.NewLine;
      }
      else
      {
        ret += "  Headers     : none" + Environment.NewLine;
      }

      if (Data != null)
      {
        ret += "  Data        : "            + Environment.NewLine;
        ret += Encoding.UTF8.GetString(Data) + Environment.NewLine;
      }
      else
      {
        ret += "  Data        : [null]" + Environment.NewLine;
      }

      return ret;
    }

    /// <summary>
    ///   Retrieve a specified header value from either the headers or the querystring.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string RetrieveHeaderValue(string key)
    {
      if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
      if (Headers != null && Headers.Count > 0)
        foreach (var curr in Headers)
        {
          if (string.IsNullOrEmpty(curr.Key)) continue;
          if (string.Compare(curr.Key.ToLower(), key.ToLower()) == 0) return curr.Value;
        }

      if (QuerystringEntries != null && QuerystringEntries.Count > 0)
        foreach (var curr in QuerystringEntries)
        {
          if (string.IsNullOrEmpty(curr.Key)) continue;
          if (string.Compare(curr.Key.ToLower(), key.ToLower()) == 0) return curr.Value;
        }

      return null;
    }

    /// <summary>
    ///   Retrieve the integer value of the last raw URL element, if found.
    /// </summary>
    /// <returns>A nullable integer.</returns>
    public int? RetrieveIdValue()
    {
      if (RawUrlEntries == null || RawUrlEntries.Count < 1) return null;
      var entries = RawUrlEntries.ToArray();
      var len = entries.Length;
      var entry = entries[len - 1];
      int ret;
      if (int.TryParse(entry, out ret)) return ret;
      return null;
    }

    /// <summary>
    ///   Create an HttpRequest object from a byte array.
    /// </summary>
    /// <param name="bytes">Byte data.</param>
    /// <returns>A populated HttpRequest.</returns>
    public static HttpRequest FromBytes(byte[] bytes)
    {
      if (bytes        == null) throw new ArgumentNullException(nameof(bytes));
      if (bytes.Length < 4) throw new ArgumentException("Too few bytes supplied to form a valid HTTP request.");

      var endOfHeader = false;
      var headerBytes = new byte[1];

      var ret = new HttpRequest();

      for (var i = 0; i < bytes.Length; i++)
      {
        if (headerBytes.Length == 1)
        {
          #region First-Byte

          headerBytes[0] = bytes[i];
          continue;

          #endregion
        }

        if (!endOfHeader && headerBytes.Length < 4)
        {
          #region Fewer-Than-Four-Bytes

          var tempHeader = new byte[i + 1];
          Buffer.BlockCopy(headerBytes, 0, tempHeader, 0, headerBytes.Length);
          tempHeader[i] = bytes[i];
          headerBytes = tempHeader;
          continue;

          #endregion
        }

        if (!endOfHeader)
        {
          #region Check-for-End-of-Header

          // check if end of headers reached
          if (
            headerBytes[headerBytes.Length - 1] == 10
         && headerBytes[headerBytes.Length - 2] == 13
         && headerBytes[headerBytes.Length - 3] == 10
         && headerBytes[headerBytes.Length - 4] == 13
          )
          {
            #region End-of-Header

            // end of headers reached
            endOfHeader = true;
            ret = BuildHeaders(headerBytes);

            #endregion
          }
          else
          {
            #region Still-Reading-Header

            var tempHeader = new byte[i + 1];
            Buffer.BlockCopy(headerBytes, 0, tempHeader, 0, headerBytes.Length);
            tempHeader[i] = bytes[i];
            headerBytes = tempHeader;

            #endregion
          }

          #endregion
        }
        else
        {
          if (ret.ContentLength > 0)
          {
            #region Append-Data

            //           1         2
            // 01234567890123456789012345
            // content-length: 5rnrnddddd
            // bytes.length = 26
            // i = 21

            if (ret.ContentLength != bytes.Length - i)
              throw new ArgumentException("Content-Length header does not match the number of data bytes.");

            ret.Data = new byte[ret.ContentLength];
            Buffer.BlockCopy(bytes, i, ret.Data, 0, (int) ret.ContentLength);
            break;

            #endregion
          }

          #region No-Data

          ret.Data = null;
          break;

          #endregion
        }
      }

      return ret;
    }

    /// <summary>
    ///   Create an HttpRequest object from a Stream.
    /// </summary>
    /// <param name="stream">Stream.</param>
    /// <returns>A populated HttpRequest.</returns>
    public static HttpRequest FromStream(Stream stream)
    {
      if (stream == null) throw new ArgumentNullException(nameof(stream));

      #region Variables

      HttpRequest ret;
      byte[] headerBytes = null;
      var lastFourBytes = new byte[4];
      lastFourBytes[0] = 0x00;
      lastFourBytes[1] = 0x00;
      lastFourBytes[2] = 0x00;
      lastFourBytes[3] = 0x00;

      #endregion

      #region Check-Stream

      if (!stream.CanRead) throw new IOException("Unable to read from stream.");

      #endregion

      #region Read-Headers

      using (var headerMs = new MemoryStream())
      {
        #region Read-Header-Bytes

        var headerBuffer = new byte[1];
        var read = 0;
        var headerBytesRead = 0;

        while ((read = stream.Read(headerBuffer, 0, headerBuffer.Length)) > 0)
          if (read > 0)
          {
            #region Initialize-Header-Bytes-if-Needed

            headerBytesRead += read;
            if (headerBytes == null) headerBytes = new byte[1];

            #endregion

            #region Update-Last-Four

            if (read == 1)
            {
              lastFourBytes[0] = lastFourBytes[1];
              lastFourBytes[1] = lastFourBytes[2];
              lastFourBytes[2] = lastFourBytes[3];
              lastFourBytes[3] = headerBuffer[0];
            }

            #endregion

            #region Append-to-Header-Buffer

            var tempHeader = new byte[headerBytes.Length + 1];
            Buffer.BlockCopy(headerBytes, 0, tempHeader, 0, headerBytes.Length);
            tempHeader[headerBytes.Length] = headerBuffer[0];
            headerBytes = tempHeader;

            #endregion

            #region Check-for-End-of-Headers

            if (lastFourBytes[0] == 13
             && lastFourBytes[1] == 10
             && lastFourBytes[2] == 13
             && lastFourBytes[3] == 10)
              break;

            #endregion
          }

        #endregion
      }

      #endregion

      #region Process-Headers

      if (headerBytes == null || headerBytes.Length < 1) throw new IOException("No header data read from the stream.");
      ret = BuildHeaders(headerBytes);

      #endregion

      #region Read-Data

      ret.Data = null;
      if (ret.ContentLength > 0)
      {
        #region Read-from-Stream

        ret.Data = new byte[ret.ContentLength];

        using (var dataMs = new MemoryStream())
        {
          var bytesRemaining = ret.ContentLength;
          long bytesRead = 0;
          var timeout = false;
          var currentTimeout = 0;

          var read = 0;
          byte[] buffer;
          long bufferSize = 2048;
          if (bufferSize > bytesRemaining) bufferSize = bytesRemaining;
          buffer = new byte[bufferSize];

          while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            if (read > 0)
            {
              dataMs.Write(buffer, 0, read);
              bytesRead = bytesRead           + read;
              bytesRemaining = bytesRemaining - read;

              // reduce buffer size if number of bytes remaining is
              // less than the pre-defined buffer size of 2KB
              if (bytesRemaining < bufferSize) bufferSize = bytesRemaining;

              buffer = new byte[bufferSize];

              // check if read fully
              if (bytesRemaining == 0) break;
              if (bytesRead      == ret.ContentLength) break;
            }
            else
            {
              if (currentTimeout >= TimeoutDataReadMs)
              {
                timeout = true;
                break;
              }

              currentTimeout += DataReadSleepMs;
              Thread.Sleep(DataReadSleepMs);
            }

          if (timeout) throw new IOException("Timeout reading data from stream.");

          ret.Data = dataMs.ToArray();
        }

        #endregion

        #region Validate-Data

        if (ret.Data == null || ret.Data.Length < 1) throw new IOException("Unable to read data from stream.");

        if (ret.Data.Length != ret.ContentLength)
          throw new IOException("Data read does not match specified content length.");

        #endregion
      }

      #endregion

      return ret;
    }

    /// <summary>
    ///   Create an HttpRequest object from a NetworkStream.
    /// </summary>
    /// <param name="stream">NetworkStream.</param>
    /// <returns>A populated HttpRequest.</returns>
    public static HttpRequest FromStream(NetworkStream stream)
    {
      if (stream == null) throw new ArgumentNullException(nameof(stream));

      #region Variables

      HttpRequest ret;
      byte[] headerBytes = null;
      var lastFourBytes = new byte[4];
      lastFourBytes[0] = 0x00;
      lastFourBytes[1] = 0x00;
      lastFourBytes[2] = 0x00;
      lastFourBytes[3] = 0x00;

      #endregion

      #region Check-Stream

      if (!stream.CanRead) throw new IOException("Unable to read from stream.");

      #endregion

      #region Read-Headers

      using (var headerMs = new MemoryStream())
      {
        #region Read-Header-Bytes

        var headerBuffer = new byte[1];
        var read = 0;
        var headerBytesRead = 0;

        while ((read = stream.Read(headerBuffer, 0, headerBuffer.Length)) > 0)
          if (read > 0)
          {
            #region Initialize-Header-Bytes-if-Needed

            headerBytesRead += read;
            if (headerBytes == null) headerBytes = new byte[1];

            #endregion

            #region Update-Last-Four

            if (read == 1)
            {
              lastFourBytes[0] = lastFourBytes[1];
              lastFourBytes[1] = lastFourBytes[2];
              lastFourBytes[2] = lastFourBytes[3];
              lastFourBytes[3] = headerBuffer[0];
            }

            #endregion

            #region Append-to-Header-Buffer

            var tempHeader = new byte[headerBytes.Length + 1];
            Buffer.BlockCopy(headerBytes, 0, tempHeader, 0, headerBytes.Length);
            tempHeader[headerBytes.Length] = headerBuffer[0];
            headerBytes = tempHeader;

            #endregion

            #region Check-for-End-of-Headers

            if (lastFourBytes[0] == 13
             && lastFourBytes[1] == 10
             && lastFourBytes[2] == 13
             && lastFourBytes[3] == 10)
              break;

            #endregion
          }

        #endregion
      }

      #endregion

      #region Process-Headers

      if (headerBytes == null || headerBytes.Length < 1) throw new IOException("No header data read from the stream.");
      ret = BuildHeaders(headerBytes);

      #endregion

      #region Read-Data

      ret.Data = null;
      if (ret.ContentLength > 0)
      {
        #region Read-from-Stream

        ret.Data = new byte[ret.ContentLength];

        using (var dataMs = new MemoryStream())
        {
          var bytesRemaining = ret.ContentLength;
          long bytesRead = 0;
          var timeout = false;
          var currentTimeout = 0;

          var read = 0;
          byte[] buffer;
          long bufferSize = 2048;
          if (bufferSize > bytesRemaining) bufferSize = bytesRemaining;
          buffer = new byte[bufferSize];

          while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            if (read > 0)
            {
              dataMs.Write(buffer, 0, read);
              bytesRead = bytesRead           + read;
              bytesRemaining = bytesRemaining - read;

              // reduce buffer size if number of bytes remaining is
              // less than the pre-defined buffer size of 2KB
              if (bytesRemaining < bufferSize) bufferSize = bytesRemaining;

              buffer = new byte[bufferSize];

              // check if read fully
              if (bytesRemaining == 0) break;
              if (bytesRead      == ret.ContentLength) break;
            }
            else
            {
              if (currentTimeout >= TimeoutDataReadMs)
              {
                timeout = true;
                break;
              }

              currentTimeout += DataReadSleepMs;
              Thread.Sleep(DataReadSleepMs);
            }

          if (timeout) throw new IOException("Timeout reading data from stream.");

          ret.Data = dataMs.ToArray();
        }

        #endregion

        #region Validate-Data

        if (ret.Data == null || ret.Data.Length < 1) throw new IOException("Unable to read data from stream.");

        if (ret.Data.Length != ret.ContentLength)
          throw new IOException("Data read does not match specified content length.");

        #endregion
      }

      #endregion

      return ret;
    }

    /// <summary>
    ///   Create an HttpRequest object from a TcpClient.
    /// </summary>
    /// <param name="client">TcpClient.</param>
    /// <returns>A populated HttpRequest.</returns>
    public static HttpRequest FromTcpClient(TcpClient client)
    {
      if (client == null) throw new ArgumentNullException(nameof(client));

      #region Variables

      HttpRequest ret;
      byte[] headerBytes = null;
      var lastFourBytes = new byte[4];
      lastFourBytes[0] = 0x00;
      lastFourBytes[1] = 0x00;
      lastFourBytes[2] = 0x00;
      lastFourBytes[3] = 0x00;

      #endregion

      #region Attach-Stream

      var stream = client.GetStream();

      if (!stream.CanRead) throw new IOException("Unable to read from stream.");

      #endregion

      #region Read-Headers

      using (var headerMs = new MemoryStream())
      {
        #region Read-Header-Bytes

        var headerBuffer = new byte[1];
        var read = 0;
        var headerBytesRead = 0;

        while ((read = stream.Read(headerBuffer, 0, headerBuffer.Length)) > 0)
          if (read > 0)
          {
            #region Initialize-Header-Bytes-if-Needed

            headerBytesRead += read;
            if (headerBytes == null) headerBytes = new byte[1];

            #endregion

            #region Update-Last-Four

            if (read == 1)
            {
              lastFourBytes[0] = lastFourBytes[1];
              lastFourBytes[1] = lastFourBytes[2];
              lastFourBytes[2] = lastFourBytes[3];
              lastFourBytes[3] = headerBuffer[0];
            }

            #endregion

            #region Append-to-Header-Buffer

            var tempHeader = new byte[headerBytes.Length + 1];
            Buffer.BlockCopy(headerBytes, 0, tempHeader, 0, headerBytes.Length);
            tempHeader[headerBytes.Length] = headerBuffer[0];
            headerBytes = tempHeader;

            #endregion

            #region Check-for-End-of-Headers

            if (lastFourBytes[0] == 13
             && lastFourBytes[1] == 10
             && lastFourBytes[2] == 13
             && lastFourBytes[3] == 10)
              break;

            #endregion
          }

        #endregion
      }

      #endregion

      #region Process-Headers

      if (headerBytes == null || headerBytes.Length < 1) throw new IOException("No header data read from the stream.");
      ret = BuildHeaders(headerBytes);

      #endregion

      #region Read-Data

      ret.Data = null;
      if (ret.ContentLength > 0)
      {
        #region Read-from-Stream

        ret.Data = new byte[ret.ContentLength];

        using (var dataMs = new MemoryStream())
        {
          var bytesRemaining = ret.ContentLength;
          long bytesRead = 0;
          var timeout = false;
          var currentTimeout = 0;

          var read = 0;
          byte[] buffer;
          long bufferSize = 2048;
          if (bufferSize > bytesRemaining) bufferSize = bytesRemaining;
          buffer = new byte[bufferSize];

          while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            if (read > 0)
            {
              dataMs.Write(buffer, 0, read);
              bytesRead = bytesRead           + read;
              bytesRemaining = bytesRemaining - read;

              // reduce buffer size if number of bytes remaining is
              // less than the pre-defined buffer size of 2KB
              if (bytesRemaining < bufferSize) bufferSize = bytesRemaining;

              buffer = new byte[bufferSize];

              // check if read fully
              if (bytesRemaining == 0) break;
              if (bytesRead      == ret.ContentLength) break;
            }
            else
            {
              if (currentTimeout >= TimeoutDataReadMs)
              {
                timeout = true;
                break;
              }

              currentTimeout += DataReadSleepMs;
              Thread.Sleep(DataReadSleepMs);
            }

          if (timeout) throw new IOException("Timeout reading data from stream.");

          ret.Data = dataMs.ToArray();
        }

        #endregion

        #region Validate-Data

        if (ret.Data == null || ret.Data.Length < 1) throw new IOException("Unable to read data from stream.");

        if (ret.Data.Length != ret.ContentLength)
          throw new IOException("Data read does not match specified content length.");

        #endregion
      }

      #endregion

      return ret;
    }

    /// <summary>
    ///   Return Post-Data as T
    /// </summary>
    /// <returns>Post-Data as T</returns>
    public T PostData<T>()
    {
      if (ContentType == "text/plain")
        return JsonConvert.DeserializeObject<T>(DecodingPlaintext(Encoding.UTF8.GetString(Data)));
      return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(Data));
    }

    private string DecodingPlaintext(string data)
    {
      var chars = data.Split(new[]{"&"}, StringSplitOptions.RemoveEmptyEntries);
      var stb = new StringBuilder();

      foreach (var c in chars)
      {
        var inner = c.Split(new[] {"="}, StringSplitOptions.RemoveEmptyEntries);
        stb.Append(HttpUtility.UrlDecode(inner[1]));
      }

      return stb.ToString();
    }

    #endregion

    #region Private-Methods

    private static HttpRequest BuildHeaders(byte[] bytes)
    {
      if (bytes == null) throw new ArgumentNullException(nameof(bytes));

      #region Initial-Values

      var ret = new HttpRequest();
      ret.TimestampUtc = DateTime.Now.ToUniversalTime();
      ret.ThreadId = Thread.CurrentThread.ManagedThreadId;
      ret.SourceIp = "unknown";
      ret.SourcePort = 0;
      ret.DestIp = "unknown";
      ret.DestPort = 0;
      ret.Headers = new Dictionary<string, string>();

      #endregion

      #region Convert-to-String-List

      var str = Encoding.UTF8.GetString(bytes);
      var headers = str.Split(new[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);

      #endregion

      #region Process-Each-Line

      for (var i = 0; i < headers.Length; i++)
        if (i == 0)
        {
          #region First-Line

          var requestLine = headers[i].Trim().Trim('\0').Split(' ');
          if (requestLine.Length < 3)
            throw new
              ArgumentException("Request line does not contain at least three parts (method, raw URL, protocol/version).");

          ret.Method = requestLine[0].ToUpper();
          ret.FullUrl = requestLine[1];
          ret.ProtocolVersion = requestLine[2];
          ret.RawUrlWithQuery = ret.FullUrl;
          ret.RawUrlWithoutQuery = ExtractRawUrlWithoutQuery(ret.RawUrlWithQuery);
          ret.RawUrlEntries = ExtractRawUrlEntries(ret.RawUrlWithoutQuery);
          ret.Querystring = ExtractQuerystring(ret.RawUrlWithQuery);
          ret.QuerystringEntries = ExtractQuerystringEntries(ret.Querystring);

          try
          {
            var uri = new Uri(ret.FullUrl);
            ret.DestHostname = uri.Host;
            ret.DestHostPort = uri.Port;
          }
          catch (Exception)
          {
          }

          if (string.IsNullOrEmpty(ret.DestHostname))
            if (!ret.FullUrl.Contains("://") & ret.FullUrl.Contains(":"))
            {
              var hostAndPort = ret.FullUrl.Split(':');
              if (hostAndPort.Length == 2)
              {
                ret.DestHostname = hostAndPort[0];
                if (!int.TryParse(hostAndPort[1], out ret.DestHostPort))
                  throw new Exception("Unable to parse destination hostname and port.");
              }
            }

          #endregion
        }
        else
        {
          #region Subsequent-Line

          var headerLine = headers[i].Split(':');
          if (headerLine.Length == 2)
          {
            var key = headerLine[0].Trim();
            var val = headerLine[1].Trim();

            if (string.IsNullOrEmpty(key)) continue;
            var keyEval = key.ToLower();

            if (keyEval.Equals("keep-alive"))
              ret.Keepalive = Convert.ToBoolean(val);
            else if (keyEval.Equals("user-agent"))
              ret.Useragent = val;
            else if (keyEval.Equals("content-length"))
              ret.ContentLength = Convert.ToInt64(val);
            else if (keyEval.Equals("content-type"))
              ret.ContentType = val;
            else
              ret.Headers = TfresCommon.AddToDict(key, val, ret.Headers);
          }

          #endregion
        }

      #endregion

      return ret;
    }

    private static string ExtractRawUrlWithoutQuery(string rawUrlWithQuery)
    {
      if (string.IsNullOrEmpty(rawUrlWithQuery)) return null;
      if (!rawUrlWithQuery.Contains("?")) return rawUrlWithQuery;
      return rawUrlWithQuery.Substring(0, rawUrlWithQuery.IndexOf("?"));
    }

    private static List<string> ExtractRawUrlEntries(string rawUrlWithoutQuery)
    {
      if (string.IsNullOrEmpty(rawUrlWithoutQuery)) return null;

      var position = 0;
      var tempString = "";
      var ret = new List<string>();

      foreach (var c in rawUrlWithoutQuery)
      {
        if (position                       == 0 &&
            string.Compare(tempString, "") == 0 &&
            c                              == '/')
          continue;

        if (c != '/' && c != '?') tempString += c;

        if (c == '/' || c == '?')
        {
          if (!string.IsNullOrEmpty(tempString)) ret.Add(tempString);

          position++;
          tempString = "";
        }
      }

      if (!string.IsNullOrEmpty(tempString)) ret.Add(tempString);

      return ret;
    }

    private static string ExtractQuerystring(string rawUrlWithQuery)
    {
      if (string.IsNullOrEmpty(rawUrlWithQuery)) return null;
      if (!rawUrlWithQuery.Contains("?")) return null;

      var qsStartPos = rawUrlWithQuery.IndexOf("?");
      if (qsStartPos >= rawUrlWithQuery.Length - 1) return null;
      return rawUrlWithQuery.Substring(qsStartPos + 1);
    }

    private static Dictionary<string, string> ExtractQuerystringEntries(string query)
    {
      if (string.IsNullOrEmpty(query)) return null;

      var ret = new Dictionary<string, string>();

      var inKey = 1;
      var inVal = 0;
      var position = 0;
      var tempKey = "";
      var tempVal = "";

      foreach (var c in query)
      {
        if (inKey == 1)
        {
          if (c != '=')
          {
            tempKey += c;
          }
          else
          {
            inKey = 0;
            inVal = 1;
            continue;
          }
        }

        if (inVal == 1)
        {
          if (c != '&')
          {
            tempVal += c;
          }
          else
          {
            inKey = 1;
            inVal = 0;

            if (!string.IsNullOrEmpty(tempVal)) tempVal = Uri.EscapeUriString(tempVal);
            ret = TfresCommon.AddToDict(tempKey, tempVal, ret);

            tempKey = "";
            tempVal = "";
            position++;
            continue;
          }
        }

        if (inVal == 1)
        {
          if (!string.IsNullOrEmpty(tempVal)) tempVal = Uri.EscapeUriString(tempVal);
          ret = TfresCommon.AddToDict(tempKey, tempVal, ret);
        }
      }

      return ret;
    }

    #endregion
  }
}