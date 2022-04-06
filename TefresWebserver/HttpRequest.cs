#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    #region Private Fields

    [JsonIgnore] private string _postData;

    #endregion

    #region Public-Members

    /// <summary>
    ///   UTC timestamp from when the request was received.
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    ///   Thread ID on which the request exists.
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    ///   The protocol and version.
    /// </summary>
    public string ProtocolVersion { get; set; }

    /// <summary>
    ///   IP address of the requestor (client).
    /// </summary>
    public string SourceIp { get; set; }

    /// <summary>
    ///   TCP port from which the request originated on the requestor (client).
    /// </summary>
    public int SourcePort { get; set; }

    /// <summary>
    ///   IP address of the recipient (server).
    /// </summary>
    public string DestIp { get; set; }

    /// <summary>
    ///   TCP port on which the request was received by the recipient (server).
    /// </summary>
    public int DestPort { get; set; }

    /// <summary>
    ///   The destination hostname as found in the request line, if present.
    /// </summary>
    public string DestHostname { get; set; }

    /// <summary>
    ///   The destination host port as found in the request line, if present.
    /// </summary>
    public int DestHostPort;

    /// <summary>
    ///   Specifies whether or not the client requested HTTP keepalives.
    /// </summary>
    public bool Keepalive { get; set; }

    /// <summary>
    ///   The HTTP method used in the request.
    /// </summary>
    public HttpVerb Verb { get; set; }

    /// <summary>
    ///   Indicates whether or not chunked transfer encoding was detected.
    /// </summary>
    public bool ChunkedTransfer { get; set; }

    /// <summary>
    ///   Indicates whether or not the payload has been gzip compressed.
    /// </summary>
    public bool Gzip { get; set; }

    /// <summary>
    ///   Indicates whether or not the payload has been deflate compressed.
    /// </summary>
    public bool Deflate { get; set; }

    /// <summary>
    ///   The full URL as sent by the requestor (client).
    /// </summary>
    public string FullUrl { get; set; }

    /// <summary>
    ///   The raw (relative) URL with the querystring attached.
    /// </summary>
    public string RawUrlWithQuery { get; set; }

    /// <summary>
    ///   The raw (relative) URL without the querystring attached.
    /// </summary>
    public string RawUrlWithoutQuery { get; set; }

    /// <summary>
    ///   List of items found in the raw URL.
    /// </summary>
    public List<string> RawUrlEntries { get; set; }

    /// <summary>
    ///   The querystring attached to the URL.
    /// </summary>
    public string Querystring { get; set; }

    /// <summary>
    ///   Dictionary containing key-value pairs from items found in the querystring.
    /// </summary>
    public Dictionary<string, string> QuerystringEntries { get; set; }

    /// <summary>
    ///   The useragent specified in the request.
    /// </summary>
    public string Useragent { get; set; }

    /// <summary>
    ///   The number of bytes in the request body.
    /// </summary>
    public long ContentLength { get; set; }

    /// <summary>
    ///   The content type as specified by the requestor (client).
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    ///   The headers found in the request.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; }

    /// <summary>
    ///   The stream from which to read the request body sent by the requestor (client).
    /// </summary>
    [JsonIgnore]
    public Stream Data { get; set; }

    /// <summary>
    ///   The original HttpListenerContext from which the HttpRequest was constructed.
    /// </summary>
    [JsonIgnore]
    public HttpListenerContext ListenerContext { get; set; }

    #endregion

    #region Constructors-and-Factories

    /// <summary>
    ///   Instantiate the object.
    /// </summary>
    public HttpRequest()
    {
      ThreadId = Thread.CurrentThread.ManagedThreadId;
      TimestampUtc = DateTime.Now.ToUniversalTime();
      QuerystringEntries = new Dictionary<string, string>();
      Headers = new Dictionary<string, string>();
    }

    /// <summary>
    ///   Instantiate the object using an HttpListenerContext.
    /// </summary>
    /// <param name="ctx">HttpListenerContext.</param>
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

      ThreadId = Thread.CurrentThread.ManagedThreadId;
      TimestampUtc = DateTime.Now.ToUniversalTime();
      ProtocolVersion = "HTTP/" + ctx.Request.ProtocolVersion;
      SourceIp = ctx.Request.RemoteEndPoint.Address.ToString();
      SourcePort = ctx.Request.RemoteEndPoint.Port;
      DestIp = ctx.Request.LocalEndPoint.Address.ToString();
      DestPort = ctx.Request.LocalEndPoint.Port;
      Verb = (HttpVerb)Enum.Parse(typeof(HttpVerb), ctx.Request.HttpMethod, true);
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

        while (RawUrlWithoutQuery.Contains("//")) RawUrlWithoutQuery = RawUrlWithoutQuery.Replace("//", "/");

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
            // skip the first slash
            continue;

          if (c != '/' && c != '?') tempString += c;

          if (c == '/' || c == '?')
          {
            if (!string.IsNullOrEmpty(tempString))
              // add to raw URL entries list
              RawUrlEntries.Add(tempString);

            position++;
            tempString = "";
          }

          if (c == '?') inQuery = 1;
        }

        if (!string.IsNullOrEmpty(tempString))
          // add to raw URL entries list
          RawUrlEntries.Add(tempString);

        #endregion

        #region Populate-Querystring

        Querystring = queryString.Length > 0 ? queryString : null;

        #endregion

        #region Parse-Querystring

        if (!string.IsNullOrEmpty(Querystring))
        {
          var inKey = 1;
          var inVal = 0;
          position = 0;
          var tempKey = "";
          var tempVal = "";

          foreach (var c in Querystring)
          {
            if (inKey == 1)
            {
              if (c == '&')
              {
                // key with no value
                if (!string.IsNullOrEmpty(tempKey))
                {
                  inKey = 1;
                  inVal = 0;

                  tempKey = WebUtility.UrlDecode(tempKey);
                  QuerystringEntries = AddToDict(tempKey, null, QuerystringEntries);

                  tempKey = "";
                  tempVal = "";
                  position++;
                  continue;
                }
              }
              else if (c != '=')
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

                tempKey = WebUtility.UrlDecode(tempKey);
                if (!string.IsNullOrEmpty(tempVal)) tempVal = WebUtility.UrlDecode(tempVal);
                QuerystringEntries = AddToDict(tempKey, tempVal, QuerystringEntries);

                tempKey = "";
                tempVal = "";
                position++;
              }
            }
          }

          if (inVal == 0)
            // val will be null
            if (!string.IsNullOrEmpty(tempKey))
            {
              tempKey = WebUtility.UrlDecode(tempKey);
              QuerystringEntries = AddToDict(tempKey, null, QuerystringEntries);
            }

          if (inVal == 1)
            if (!string.IsNullOrEmpty(tempKey))
            {
              tempKey = WebUtility.UrlDecode(tempKey);
              if (!string.IsNullOrEmpty(tempVal)) tempVal = WebUtility.UrlDecode(tempVal);
              QuerystringEntries = AddToDict(tempKey, tempVal, QuerystringEntries);
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
        var uri = new Uri(FullUrl);
        DestHostname = uri.Host;
        DestHostPort = uri.Port;
      }
      catch
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
        Headers = AddToDict(key, val, Headers);
      }

      foreach (var curr in Headers)
      {
        if (string.IsNullOrEmpty(curr.Key)) continue;
        if (string.IsNullOrEmpty(curr.Value)) continue;

        if (curr.Key.ToLower().Equals("transfer-encoding"))
        {
          if (curr.Value.ToLower().Contains("chunked"))
            ChunkedTransfer = true;
          if (curr.Value.ToLower().Contains("gzip"))
            Gzip = true;
          if (curr.Value.ToLower().Contains("deflate"))
            Deflate = true;
        }
      }

      #endregion

      #region Payload

      Data = ctx.Request.InputStream;

      #endregion

      #endregion
    }


    #region Public-Methods

    /// <summary>
    ///   Retrieve a string-formatted, human-readable copy of the HttpRequest instance.
    /// </summary>
    /// <returns>String-formatted, human-readable copy of the HttpRequest instance.</returns>
    public override string ToString()
    {
      var ret = "";

      ret += "--- HTTP Request ---" + Environment.NewLine;
      ret += TimestampUtc.ToString("MM/dd/yyyy HH:mm:ss") + " " + SourceIp + ":" + SourcePort + " to " + DestIp + ":" +
             DestPort + Environment.NewLine;
      ret += "  "               + Verb + " " + RawUrlWithoutQuery + " " + ProtocolVersion + Environment.NewLine;
      ret += "  Full URL    : " + FullUrl + Environment.NewLine;
      ret += "  Raw URL     : " + RawUrlWithoutQuery + Environment.NewLine;
      ret += "  Querystring : " + Querystring + Environment.NewLine;
      ret += "  Useragent   : " + Useragent + " (Keepalive " + Keepalive + ")" + Environment.NewLine;
      ret += "  Content     : " + ContentType + " (" + ContentLength + " bytes)" + Environment.NewLine;
      ret += "  Destination : " + DestHostname + ":" + DestHostPort + Environment.NewLine;

      if (Headers != null && Headers.Count > 0)
      {
        ret += "  Headers     : " + Environment.NewLine;
        foreach (var curr in Headers) ret += "    " + curr.Key + ": " + curr.Value + Environment.NewLine;
      }
      else
      {
        ret += "  Headers     : none" + Environment.NewLine;
      }

      return ret;
    }

    /// <summary>
    ///   Retrieve a specified header value from either the headers or the querystring (case insensitive).
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
    ///   For chunked transfer-encoded requests, read the next chunk.
    /// </summary>
    /// <returns>Chunk.</returns>
    public async Task<Chunk> ReadChunk()
    {
      if (!ChunkedTransfer) throw new IOException("Request is not chunk transfer-encoded.");

      var chunk = new Chunk();

      #region Get-Length-and-Metadata

      var buffer = new byte[1];
      byte[] lenBytes = null;
      int bytesRead;

      while (true)
      {
        bytesRead = await Data.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead > 0)
        {
          lenBytes = AppendBytes(lenBytes, buffer);
          var lenStr = Encoding.UTF8.GetString(lenBytes);

          if (lenBytes[lenBytes.Length - 1] == 10)
          {
            lenStr = lenStr.Trim();

            if (lenStr.Contains(";"))
            {
              var lenStrParts = lenStr.Split(new[] { ';' }, 2);

              if (lenStrParts.Length == 2) chunk.Metadata = lenStrParts[1];
            }
            else
            {
              chunk.Length = int.Parse(lenStr, NumberStyles.HexNumber);
            }

            // Console.WriteLine("- Chunk length determined: " + chunk.Length); 
            break;
          }
        }
      }

      #endregion

      #region Get-Data

      // Console.WriteLine("- Reading " + chunk.Length + " bytes");

      if (chunk.Length > 0)
      {
        chunk.IsFinalChunk = false;
        buffer = new byte[chunk.Length];
        bytesRead = await Data.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead == chunk.Length)
        {
          chunk.Data = new byte[chunk.Length];
          Buffer.BlockCopy(buffer, 0, chunk.Data, 0, chunk.Length);
          // Console.WriteLine("- Data: " + Encoding.UTF8.GetString(buffer));
        }
        else
        {
          throw new IOException("Expected " + chunk.Length + " bytes but only read " + bytesRead + " bytes in chunk.");
        }
      }
      else
      {
        chunk.IsFinalChunk = true;
      }

      #endregion

      #region Get-Trailing-CRLF

      buffer = new byte[1];

      while (true)
      {
        bytesRead = await Data.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead > 0)
          if (buffer[0] == 10)
            break;
      }

      #endregion

      return chunk;
    }

    /// <summary>
    ///   Return Post-Data as T
    /// </summary>
    /// <returns>Post-Data as T</returns>
    public T PostData<T>() => JsonConvert.DeserializeObject<T>(PostDataAsString);


    public string PostDataAsString
    {
      get
      {
        if (_postData == null)
          using (var reader = new StreamReader(Data, Encoding.UTF8))
            _postData = reader.ReadToEnd();

        return _postData;
      }
    }

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

        var split = GetDataAsString.Split(new[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
        var res = new Dictionary<string, string>();
        foreach (var x in split)
          try
          {
            var entry = x.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
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
    public string GetDataAsString => Querystring;

    /// <summary>
    ///   If a JavaScript/Webbrowser sends multiple files - you can read all files at once
    /// </summary>
    /// <param name="uploadLimit">Maximal complete size of all files</param>
    /// <param name="encoding">Set file encoding</param>
    public List<HttpRequestFile> GetUploadedFiles(int uploadLimit = int.MaxValue, Encoding encoding = null)
    {
      if (encoding == null)
        encoding = Encoding.UTF8;

      try
      {
        var res = new List<HttpRequestFile>();
        HttpRequestFile current = null;

        using (var reader = new StreamReader(Data))
        {
          string end = null;
          while (!reader.EndOfStream)
          {
            var line = reader.ReadLine();
            if (end == null)
              end = line;

            if (line.StartsWith(end))
            {
              if (current != null)
              {
                current.Finalize(encoding);
                res.Add(current);
              }

              // Checking EndOfStream seems to be nasty and redundant
              // But: works well with different browser implementations and nasty clients.
              if (reader.EndOfStream)
                break;
              var head1 = reader.ReadLine();
              if (reader.EndOfStream)
                break;
              var head2 = reader.ReadLine();
              if (reader.EndOfStream)
                break;

              current = new HttpRequestFile(head1, head2);
              reader.ReadLine();
              continue;
            }

            current?.AddLine(line);
          }
        }

        return res;
      }
      catch
      {
        return null;
      }
    }

    #endregion

    #region Private-Methods

    private static HttpRequest BuildHeaders(byte[] bytes)
    {
      if (bytes == null) throw new ArgumentNullException(nameof(bytes));

      #region Initial-Values

      var ret = new HttpRequest
      {
        TimestampUtc = DateTime.Now.ToUniversalTime(),
        ThreadId = Thread.CurrentThread.ManagedThreadId,
        SourceIp = "unknown",
        SourcePort = 0,
        DestIp = "unknown",
        DestPort = 0,
        Headers = new Dictionary<string, string>()
      };

      #endregion

      #region Convert-to-String-List

      var str = Encoding.UTF8.GetString(bytes);
      var headers = str.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

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

          ret.Verb = (HttpVerb)Enum.Parse(typeof(HttpVerb), requestLine[0], true);
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
          catch
          {
            // ignore
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
            {
              ret.Keepalive = Convert.ToBoolean(val);
            }
            else if (keyEval.Equals("user-agent"))
            {
              ret.Useragent = val;
            }
            else if (keyEval.Equals("content-length"))
            {
              ret.ContentLength = Convert.ToInt64(val);
            }
            else if (keyEval.Equals("content-type"))
            {
              ret.ContentType = val;
            }
            else if (keyEval.Equals("transfer-encoding"))
            {
              if (string.IsNullOrEmpty(val)) continue;
              if (val.ToLower().Contains("chunked"))
                ret.ChunkedTransfer = true;
              if (val.ToLower().Contains("gzip"))
                ret.Gzip = true;
              if (val.ToLower().Contains("deflate"))
                ret.Deflate = true;
            }
            else
            {
              ret.Headers = AddToDict(key, val, ret.Headers);
            }
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

      foreach (var c in rawUrlWithoutQuery.Where(c => position != 0 || string.CompareOrdinal(tempString, "") != 0 ||
                                                      c        != '/'))
      {
        if (c != '/' && c != '?') tempString += c;

        if (c != '/' && c != '?') continue;
        if (!string.IsNullOrEmpty(tempString))
          // add to raw URL entries list
          ret.Add(tempString);

        position++;
        tempString = "";
      }

      if (!string.IsNullOrEmpty(tempString))
        // add to raw URL entries list
        ret.Add(tempString);

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

            if (!string.IsNullOrEmpty(tempVal)) tempVal = WebUtility.UrlEncode(tempVal);
            ret = AddToDict(tempKey, tempVal, ret);

            tempKey = "";
            tempVal = "";
            position++;
            continue;
          }
        }

        if (inVal == 1)
        {
          if (!string.IsNullOrEmpty(tempVal)) tempVal = WebUtility.UrlEncode(tempVal);
          ret = AddToDict(tempKey, tempVal, ret);
        }
      }

      return ret;
    }

    private static Dictionary<string, string> AddToDict(string key, string val, Dictionary<string, string> existing)
    {
      if (string.IsNullOrEmpty(key)) return existing;

      var ret = new Dictionary<string, string>();

      if (existing == null)
      {
        ret.Add(key, val);
        return ret;
      }

      if (existing.ContainsKey(key))
      {
        if (string.IsNullOrEmpty(val)) return existing;
        var tempVal = existing[key];
        tempVal += "," + val;
        existing.Remove(key);
        existing.Add(key, tempVal);
        return existing;
      }

      existing.Add(key, val);
      return existing;
    }

    private static byte[] AppendBytes(byte[] orig, byte[] append)
    {
      if (orig == null && append == null) return null;

      byte[] ret;

      if (append == null)
      {
        ret = new byte[orig.Length];
        Buffer.BlockCopy(orig, 0, ret, 0, orig.Length);
        return ret;
      }

      if (orig == null)
      {
        ret = new byte[append.Length];
        Buffer.BlockCopy(append, 0, ret, 0, append.Length);
        return ret;
      }

      ret = new byte[orig.Length + append.Length];
      Buffer.BlockCopy(orig, 0, ret, 0, orig.Length);
      Buffer.BlockCopy(append, 0, ret, orig.Length, append.Length);
      return ret;
    }

    #endregion

    internal void Close()
    {
      try
      {
        Data.Close();
      }
      catch
      {
        // ignore
      }
    }
  }
}