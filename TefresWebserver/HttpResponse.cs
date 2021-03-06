﻿#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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

    #endregion

    #region Constructors-and-Factories

    /// <summary>
    ///   Instantiate the object.
    /// </summary>
    private HttpResponse()
    {
    }

    internal HttpResponse(HttpRequest req, HttpListenerContext ctx)
    {
      _request = req ?? throw new ArgumentNullException(nameof(req));
      _response = ctx?.Response ?? throw new ArgumentNullException(nameof(ctx));
      _outputStream = _response.OutputStream;
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

      ret += "--- HTTP Response ---" + Environment.NewLine;
      ret += "  Status Code        : " + StatusCode + Environment.NewLine;
      ret += "  Status Description : " + HttpStatusHelper.GetStatusMessage(StatusCode) + Environment.NewLine;
      ret += "  Content            : " + ContentType + Environment.NewLine;
      ret += "  Content Length     : " + ContentLength + " bytes" + Environment.NewLine;
      ret += "  Chunked Transfer   : " + true + Environment.NewLine;
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
    /// <returns>True if successful.</returns>
    public async Task<bool> Send()
    {
      if (!_headersSent) SendHeaders();

      await _outputStream.FlushAsync();
      _outputStream.Close();

      _response?.Close();
      return true;
    }

    /// <summary>
    /// Send headers (statusCode) and no data to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <returns>True if successful.</returns>
    public Task<bool> Send(int statusCode)
    {
      StatusCode = statusCode;
      return Send();
    }

    /// <summary>
    /// Send headers (statusCode) and no data to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <returns>True if successful.</returns>
    public Task<bool> Send(HttpStatusCode statusCode)
    {
      StatusCode = (int)statusCode;
      return Send();
    }

    /// <summary>
    /// Send headers (statusCode) and a error message to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <param name="errorMessage">Plaintext error message</param>
    /// <returns>True if successful.</returns>
    [Obsolete("Please use Send(HttpStatusCode statusCode, string errorMessage, int errorCode, string helpUrl) for more polite error messages.")]
    public Task Send(HttpStatusCode statusCode, string errorMessage)
    {
      return Send((int) statusCode, errorMessage);
    }

    /// <summary>
    /// Send headers (statusCode) and a content to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <param name="content">Plaintext content</param>
    /// <param name="mimeType">Content Mime-Type</param>
    /// <returns>True if successful.</returns>
    public Task Send(HttpStatusCode statusCode, string content, string mimeType)
    {
      return Send((int)statusCode, content, mimeType);
    }

    /// <summary>
    /// Send headers (statusCode) and a error message to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <param name="errorMessage">Plaintext error message</param>
    /// <param name="errorCode">Unique error Code</param>
    /// /// <param name="helpUrl">Link to a help/documentation to fix the problem.</param>
    /// <returns>True if successful.</returns>
    public Task Send(HttpStatusCode statusCode, string errorMessage, int errorCode, string helpUrl)
    {
      return Send((int)statusCode, errorMessage, errorCode, helpUrl);
    }

    /// <summary>
    /// Send headers (statusCode) and a error message to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <param name="errorMessage">Plaintext error message</param>
    /// <returns>True if successful.</returns>
    [Obsolete("Please use Send(HttpStatusCode statusCode, string errorMessage, int errorCode, string helpUrl) for more polite error messages.")]
    public Task<bool> Send(int statusCode, string errorMessage)
    {
      StatusCode = statusCode;
      return SendAsync(Encoding.UTF8.GetBytes(errorMessage), "text/plain");
    }

    /// <summary>
    /// Send headers (statusCode / mimeType) and a content to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <param name="content">Plaintext content (utf-8)</param>
    /// <param name="mimeType">Content Mime-Type</param>
    /// <returns>True if successful.</returns>
    public Task<bool> Send(int statusCode, string content, string mimeType)
    {
      StatusCode = statusCode;
      return SendAsync(Encoding.UTF8.GetBytes(content), mimeType);
    }

    /// <summary>
    /// Send headers (statusCode) and a error message to the requestor and terminate the connection.
    /// </summary>
    /// <param name="statusCode">StatusCode</param>
    /// <param name="errorMessage">Plaintext error message</param>
    /// <param name="errorCode">Unique error Code</param>
    /// /// <param name="helpUrl">Link to a help/documentation to fix the problem.</param>
    /// <returns>True if successful.</returns>
    public Task<bool> Send(int statusCode, string errorMessage, int errorCode, string helpUrl)
    {
      StatusCode = statusCode;
      return Send(new ErrorInfoMessage
      {
        ErrorCode = errorCode,
        ErrorHelpUrl = helpUrl,
        ErrorMessage = errorMessage,
        HttpStatusCode = statusCode
      });
    }

    /// <summary>
    ///   Send headers and data to the requestor and terminate the connection.
    /// </summary>
    /// <param name="obj">Object.</param>
    /// <returns>True if successful.</returns>
    public Task<bool> Send(object obj) 
      => SendAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)), "application/json");

    /// <summary>
    ///   Send headers and data to the requestor and terminate the connection.
    /// </summary>
    /// <param name="data">Data.</param>
    /// <param name="mimeType">Force a special MIME-Type</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> Send(string data, string mimeType = "application/json") 
      => await SendAsync(Encoding.UTF8.GetBytes(data), mimeType);

    /// <summary>
    ///   Send headers and data to the requestor and terminate the connection.
    /// </summary>
    /// <param name="data">Data.</param>
    /// <param name="mimeType">Force a special MIME-Type</param>
    /// <returns>True if successful.</returns>
    public Task<bool> Send(byte[] data, string mimeType)
      => SendAsync(data, mimeType);

    /// <summary>
    ///   Send headers and data to the requestor and terminate the connection.
    /// </summary>
    /// <param name="data">Data.</param>
    /// <param name="mimeType">Force a special MIME-Type</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> SendAsync(byte[] data, string mimeType)
    {
      ContentType = mimeType;

      try
      {
        using (var ms = new MemoryStream(data))
          await Send(data.Length, ms);
        return true;
      }
      catch
      {
        // do nothing
        return false;
      }
    }

    private const int _bufferSize = 1024 * 1024;

    /// <summary>
    ///   Send headers and data to the requestor and terminate.
    /// </summary>
    /// <param name="contentLength">Number of bytes to send.</param>
    /// <param name="stream">Stream containing the data.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> Send(long contentLength, Stream stream)
    {
      if (!_headersSent)
        SendHeaders();
      ContentLength = contentLength;

      try
      {
        if (stream != null && stream.CanRead && contentLength > 0)
        {
          var buffer = new byte[_bufferSize];
          var read = 0;
          do
          {
            read = stream.Read(buffer, 0, buffer.Length);
            if (read > 0)
              await SendChunk(buffer, read);
          } while (read != 0);
        }
      }
      catch
      {
        // do nothing
        return false;
      }
      finally
      {
        await SendFinalChunk();
      }

      return true;
    }

    /// <summary>
    ///   Send headers (if not already sent) and a chunk of data using chunked transfer-encoding, and keep the connection
    ///   in-tact.
    /// </summary>
    /// <param name="chunk">Chunk of data.</param>
    /// <param name="encoding">Chunk (string) encoding (default = UTF-8)</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> SendChunk(string chunk, Encoding encoding = null)
    {
      if (!_headersSent)
        SendHeaders();
      return await SendChunk(encoding == null ? Encoding.UTF8.GetBytes(chunk) : encoding.GetBytes(chunk));
    }

    /// <summary>
    ///   Send headers (if not already sent) and a chunk of data using chunked transfer-encoding, and keep the connection
    ///   in-tact.
    /// </summary>
    /// <param name="chunk">Chunk of data.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> SendChunk(byte[] chunk)
    {
      if (!_headersSent)
        SendHeaders();
      return await SendChunk(chunk, chunk.Length);
    }

    /// <summary>
    ///   Send headers (if not already sent) and a chunk of data using chunked transfer-encoding, and keep the connection
    ///   in-tact.
    /// </summary>
    /// <param name="chunk">Chunk of data.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> SendChunk(byte[] chunk, int length)
    {
      try
      {
        if (chunk == null || chunk.Length < 1)
          chunk = new byte[0];
        _outputStream.Write(chunk, 0, length);
      }
      catch
      {
        // do nothing
        return false;
      }

      return true;
    }

    /// <summary>
    ///   Send headers (if not already sent) and the final chunk of data using chunked transfer-encoding and terminate the
    ///   connection.
    /// </summary>
    /// <returns>True if successful.</returns>
    public async Task<bool> SendFinalChunk()
    {
      return await SendFinalChunk(null, 0);
    }

    /// <summary>
    ///   Send headers (if not already sent) and the final chunk of data using chunked transfer-encoding and terminate the
    ///   connection.
    /// </summary>
    /// <param name="chunk">Chunk of data.</param>
    /// <param name="encoding">Chunk (string) encoding (default: UTF-8)</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> SendFinalChunk(string chunk, Encoding encoding = null)
    {
      return await SendFinalChunk(encoding == null ? Encoding.UTF8.GetBytes(chunk) : encoding.GetBytes(chunk));
    }

    /// <summary>
    ///   Send headers (if not already sent) and the final chunk of data using chunked transfer-encoding and terminate the
    ///   connection.
    /// </summary>
    /// <param name="chunk">Chunk of data.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> SendFinalChunk(byte[] chunk)
    {
      return await SendFinalChunk(chunk, chunk?.Length ?? 0);
    }

    /// <summary>
    ///   Send headers (if not already sent) and the final chunk of data using chunked transfer-encoding and terminate the
    ///   connection.
    /// </summary>
    /// <param name="chunk">Chunk of data.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> SendFinalChunk(byte[] chunk, int length)
    {
      try
      {
        if (!_headersSent)
          SendHeaders();

        if (chunk != null && length > 0) await _outputStream.WriteAsync(chunk, 0, length);

        var endChunk = new byte[0];
        await _outputStream.WriteAsync(endChunk, 0, endChunk.Length);
      }
      catch
      {
        // do nothing
        return false;
      }
      finally
      {
        await _outputStream.FlushAsync();
        _outputStream.Close();

        _response?.Close();
      }

      return true;
    }

    #endregion

    #region Private-Methods

    private void SendHeaders()
    {
      if (_headersSent) throw new IOException("Headers already sent.");

      try
      {
        _response.ContentLength64 = ContentLength;
        _response.StatusCode = StatusCode;
        _response.StatusDescription = HttpStatusHelper.GetStatusMessage(StatusCode);
        _response.SendChunked = true;
        _response.AddHeader("Access-Control-Allow-Origin", "*");
        _response.ContentType = ContentType;

        if (Headers != null && Headers.Count > 0)
          foreach (var curr in Headers)
          {
            if (string.IsNullOrEmpty(curr.Key)) continue;
            _response.AddHeader(curr.Key, curr.Value);
          }
      }
      catch
      {
        // ignore
      }

      _headersSent = true;
    }

    private byte[] PackageChunk(byte[] chunk)
    {
      if (chunk == null || chunk.Length < 1) return Encoding.UTF8.GetBytes("0\r\n\r\n");

      var ms = new MemoryStream();

      var newlineStr = "\r\n";
      var newline = Encoding.UTF8.GetBytes(newlineStr);

      var chunkLenHex = chunk.Length.ToString("X");
      var chunkLen = Encoding.UTF8.GetBytes(chunkLenHex);

      ms.Write(chunkLen, 0, chunkLen.Length);
      ms.Write(newline, 0, newline.Length);
      ms.Write(chunk, 0, chunk.Length);
      ms.Write(newline, 0, newline.Length);
      ms.Seek(0, SeekOrigin.Begin);

      var ret = ms.ToArray();

      return ret;
    }

    #endregion
  }
}