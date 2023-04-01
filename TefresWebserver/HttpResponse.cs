#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

#endregion

namespace Tfres
{
  /// <summary>
  ///   Response to an HTTP request.
  /// </summary>
  public class HttpResponse
  {
    #region Public-Members

    /// <summary>
    ///   The HTTP status code to return to the requestor (client).
    /// </summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>
    ///   User-supplied headers to include in the response.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    ///   User-supplied content-type to include in the response.
    /// </summary>
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    ///   The length of the supplied response data.
    /// </summary>
    public long ContentLength { get; set; }

    #endregion

    #region Private-Members

    private readonly HttpRequest _request;
    private readonly HttpListenerResponse _response;
    private readonly Stream _outputStream;
    private bool _headersSent;
    private readonly JsonSerializer _serializer;

    #endregion

    #region Constructors-and-Factories

    /// <summary>
    ///   Instantiate the object.
    /// </summary>
    private HttpResponse()
    {
    }

    internal HttpResponse(HttpRequest req, HttpListenerContext ctx, JsonSerializer serializer)
    {
      _request = req ?? throw new ArgumentNullException(nameof(req));
      _response = ctx?.Response ?? throw new ArgumentNullException(nameof(ctx));
      _outputStream = _response.OutputStream;
      _serializer = serializer;
    }

    #endregion

    #region Public-Methods

    /// <summary>
    ///   Retrieve a string-formatted, human-readable copy of the HttpResponse instance.
    /// </summary>
    /// <returns>String-formatted, human-readable copy of the HttpResponse instance.</returns>
    public override string ToString()
    {
      var ret = "";

      ret += "--- HTTP Response ---"   + Environment.NewLine;
      ret += "  Status Code        : " + StatusCode    + Environment.NewLine;
      ret += "  Content            : " + ContentType   + Environment.NewLine;
      ret += "  Content Length     : " + ContentLength + " bytes" + Environment.NewLine;
      ret += "  Chunked Transfer   : " + true          + Environment.NewLine;
      if (Headers != null && Headers.Count > 0)
      {
        ret += "  Headers            : " + Environment.NewLine;
        foreach (var curr in Headers) ret += "  - " + curr.Key + ": " + curr.Value + Environment.NewLine;
      }
      else
      {
        ret += "  Headers          : none" + Environment.NewLine;
      }

      return ret;
    }

    /// <summary>
    ///   Send headers and no data to the requestor and terminate the connection.
    /// </summary>
    public void Send()
    {
      if (!_outputStream.CanWrite)
        return;

      SendHeaders(false);

      _outputStream.Flush();
      _outputStream.Close();

      _response?.Close();
    }

    /// <summary>
    ///   Send headers (statusCode) and no data to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    public void Send(int statusCode)
    {
      StatusCode = statusCode;
      Send();
    }

    /// <summary>
    ///   Send headers (statusCode) and no data to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    public void Send(HttpStatusCode statusCode)
    {
      StatusCode = (int)statusCode;
      Send();
    }

    /// <summary>
    ///   Send headers (statusCode) and a error message to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <param name="errorMessage">Plaintext error message</param>
    [Obsolete("Please use Send(HttpStatusCode statusCode, string errorMessage, int errorCode, string helpUrl) for more polite error messages.")]
    public void Send(HttpStatusCode statusCode, string errorMessage)
      => Send((int)statusCode, errorMessage);

    /// <summary>
    ///   Send headers (statusCode) and a content to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <param name="content">Plaintext content</param>
    /// <param name="mimeType">Content Mime-Type</param>
    public void Send(HttpStatusCode statusCode, string content, string mimeType)
      => Send((int)statusCode, content, mimeType);

    /// <summary>
    ///   Send headers (statusCode) and a error message to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <param name="errorMessage">Plaintext error message</param>
    /// <param name="errorCode">Unique error Code</param>
    /// ///
    /// <param name="helpUrl">Link to a help/documentation to fix the problem.</param>
    public void Send(HttpStatusCode statusCode, string errorMessage, int errorCode, string helpUrl)
      => Send((int)statusCode, errorMessage, errorCode, helpUrl);

    /// <summary>
    ///   Send headers (statusCode) and a error message to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <param name="errorMessage">Plaintext error message</param>
    [Obsolete("Please use Send(HttpStatusCode statusCode, string errorMessage, int errorCode, string helpUrl) for more polite error messages.")]
    public void Send(int statusCode, string errorMessage)
    {
      StatusCode = statusCode;
      Send(errorMessage == null ? null : Encoding.UTF8.GetBytes(errorMessage), "text/plain");
    }

    /// <summary>
    ///   Send headers (statusCode / mimeType) and a content to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <param name="content">Plaintext content (utf-8)</param>
    /// <param name="mimeType">Content Mime-Type</param>
    public void Send(int statusCode, string content, string mimeType)
    {
      StatusCode = statusCode;
      Send(content == null ? null : Encoding.UTF8.GetBytes(content), mimeType);
    }

    /// <summary>
    ///   Send headers (statusCode) and a error message to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <param name="errorMessage">Plaintext error message</param>
    /// <param name="errorCode">Unique error Code</param>
    /// ///
    /// <param name="helpUrl">Link to a help/documentation to fix the problem.</param>
    public void Send(int statusCode, string errorMessage, int errorCode, string helpUrl)
    {
      if (!_outputStream.CanWrite)
        return;

      StatusCode = statusCode;
      Send(new ErrorInfoMessage
      {
        ErrorCode = errorCode,
        ErrorHelpUrl = helpUrl ?? "",
        ErrorMessage = errorMessage ?? "",
        HttpStatusCode = statusCode
      });
    }

    /// <summary>
    ///   Send headers and data to the requestor and terminate the connection.
    /// </summary>
    /// <param name="obj">Object.</param>
    /// <returns>True if successful.</returns>
    public void Send(object obj)
    {
      if (!_outputStream.CanWrite)
        return;

      if (obj == null)
        Send();

      using (var ms = new MemoryStream())
      {
        using (var writer = new StreamWriter(ms, Encoding.UTF8, 4096, true))
          _serializer.Serialize(writer, obj);
        ms.Seek(0, SeekOrigin.Begin);
        Send(ms, "application/json");
      }
    }

    /// <summary>
    ///   Send headers and data to the requestor and terminate the connection.
    /// </summary>
    /// <param name="data">Data.</param>
    /// <param name="mimeType">Force a special MIME-Type</param>
    /// <returns>True if successful.</returns>
    public void Send(string data, string mimeType = "application/json")
      => Send(data == null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(data), mimeType);

    /// <summary>
    ///   Send headers and data to the requestor and terminate the connection.
    /// </summary>
    /// <param name="data">Data.</param>
    /// <param name="mimeType">Force a special MIME-Type</param>
    /// <returns>True if successful.</returns>
    public void Send(byte[] data, string mimeType)
    {
      if (!_outputStream.CanWrite)
        return;

      if (data == null)
        data = Array.Empty<byte>();

      using (var ms = new MemoryStream(data))
        Send(ms, mimeType);
    }

    /// <summary>
    ///   Send headers and data to the requestor and terminate the connection.
    /// </summary>
    /// <param name="path">Path to file</param>
    /// <param name="mimeType">Force a special MIME-Type</param>
    /// <returns>True if successful.</returns>
    public void SendFile(string path, string mimeType = "application/octet-stream")
    {
      if (!_outputStream.CanWrite)
        return;

      ContentType = mimeType;

      try
      {
        using (var ms = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
          Send(ms, mimeType);
      }
      catch
      {
        SendFinalChunk();
      }
    }

    private const int _bufferSize = 1024 * 1024;

    /// <summary>
    ///   Send headers and data to the requestor and terminate.
    /// </summary>
    /// <param name="stream">Stream containing the data.</param>
    /// <param name="mimeType"></param>
    /// <returns>True if successful.</returns>
    public void Send(Stream stream, string mimeType)
    {
      if (!_outputStream.CanWrite)
        return;

      ContentType = mimeType;
      SendHeaders(true);

      try
      {
        var buffer = new byte[_bufferSize];

        int read;
        do
        {
          read = stream.Read(buffer, 0, buffer.Length);
          if (read > 0)
            SendChunk(buffer, read);
        } while (read != 0);
      }
      catch
      {
        // Do nothing
      }

      SendFinalChunk();
    }

    /// <summary>
    ///   Send headers (if not already sent) and a chunk of data using chunked transfer-encoding, and keep the connection
    ///   in-tact.
    /// </summary>
    /// <param name="chunk">Chunk of data.</param>
    /// <param name="encoding">Chunk (string) encoding (default = UTF-8)</param>
    /// <param name="mimeType">Force a special MIME-Type</param>
    /// <returns>True if successful.</returns>
    public void SendChunk(string chunk, Encoding encoding = null, string mimeType = "application/octet-stream")
    {
      if (!_outputStream.CanWrite)
        return;

      SendHeaders(true);

      if (chunk == null)
        return;

      SendChunk(encoding == null ? Encoding.UTF8.GetBytes(chunk) : encoding.GetBytes(chunk), mimeType);
    }

    /// <summary>
    ///   Send headers (if not already sent) and a chunk of data using chunked transfer-encoding, and keep the connection
    ///   in-tact.
    /// </summary>
    /// <param name="chunk">Chunk of data.</param>
    /// <param name="mimeType">Force a special MIME-Type</param>
    /// <returns>True if successful.</returns>
    public void SendChunk(byte[] chunk, string mimeType = "application/octet-stream")
    {
      if (!_outputStream.CanWrite)
        return;

      ContentType = mimeType;

      if (!_headersSent)
        SendHeaders(true);

      if (chunk == null)
        return;

      SendChunk(chunk, chunk.Length);
    }

    /// <summary>
    ///   Send headers (if not already sent) and a chunk of data using chunked transfer-encoding, and keep the connection
    ///   in-tact.
    /// </summary>
    /// <param name="chunk">Chunk of data.</param>
    /// <returns>True if successful.</returns>
    public void SendChunk(byte[] chunk, int length)
    {
      if (!_outputStream.CanWrite)
        return;

      SendHeaders(true);
      if (chunk == null)
        return;
      if(!_outputStream.CanWrite)
        return;

      if (chunk.Length > 0)
        ContentLength += chunk.Length;

      if (chunk.Length < 1) chunk = Array.Empty<byte>();
      _outputStream.Write(chunk, 0, length);
    }

    /// <summary>
    ///   Send headers (if not already sent) and the final chunk of data using chunked transfer-encoding and terminate the
    ///   connection.
    /// </summary>
    /// <returns>True if successful.</returns>
    public void SendFinalChunk() => SendFinalChunk(null);

    /// <summary>
    ///   Send headers (if not already sent) and the final chunk of data using chunked transfer-encoding and terminate the
    ///   connection.
    /// </summary>
    /// <param name="chunk">Chunk of data.</param>
    /// <param name="encoding">Chunk (string) encoding (default: UTF-8)</param>
    /// <returns>True if successful.</returns>
    public void SendFinalChunk(string chunk, Encoding encoding = null)
      => SendFinalChunk(encoding == null ? Encoding.UTF8.GetBytes(chunk) : encoding.GetBytes(chunk));

    /// <summary>
    ///   Send headers (if not already sent) and the final chunk of data using chunked transfer-encoding and terminate the
    ///   connection.
    /// </summary>
    /// <param name="chunk">Chunk of data.</param>
    /// <returns>True if successful.</returns>
    public void SendFinalChunk(byte[] chunk)
    {
      if (!_outputStream.CanWrite)
        return;

      SendHeaders(true);

      if (chunk != null && chunk.Length > 0)
        ContentLength += chunk.Length;

      if (chunk != null && chunk.Length > 0)
        _outputStream.Write(chunk, 0, chunk.Length);

      var endChunk = Array.Empty<byte>();
      _outputStream.Write(endChunk, 0, endChunk.Length);
      _outputStream.Flush();

      _outputStream.Close();
      _response?.Close();
    }

    #endregion

    #region Private-Methods

    private object _sendHeadersLock = new object();

    private void SendHeaders(bool isChunked)
    {
      lock (_sendHeadersLock)
      {
        if (!_outputStream.CanWrite)
          return;

        if (_headersSent)
          return;

        try
        {
          _response.ContentLength64 = ContentLength;
          _response.StatusCode = StatusCode;
          _response.SendChunked = isChunked;
          _response.AddHeader("Access-Control-Allow-Origin", "*");
          _response.ContentType = ContentType;

          if (Headers != null && Headers.Count > 0)
            foreach (var c in Headers.Where(c => !string.IsNullOrEmpty(c.Key)))
              _response.AddHeader(c.Key, c.Value);
        }
        catch
        {
          // ignore
        }

        _headersSent = true;
      }
    }

    #endregion

    public void Close()
    {
      try
      {
        _outputStream.Close();
        _response?.Close();
      }
      catch
      {
        // ignore
      }
    }
  }
}