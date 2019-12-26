#region

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

#endregion

namespace Tfres
{
  public class HttpResponse
  {
    #region Public-Methods

    /// <summary>
    ///   Retrieve a string-formatted, human-readable copy of the HttpResponse instance.
    /// </summary>
    /// <returns>String-formatted, human-readable copy of the HttpResponse instance.</returns>
    public override string ToString()
    {
      var ret = "";

      ret += "--- HTTP Response ---" + Environment.NewLine;
      ret += _timestampUtc.ToString("MM/dd/yyyy HH:mm:ss") + " "  + _sourceIp + ":" + _sourcePort + " to " +
             _destIp                                       + ":"  +
             _destPort                                     + "  " + _method + " " + _rawUrlWithoutQuery +
             Environment.NewLine;
      ret += "  Success : " + _success                                        + Environment.NewLine;
      ret += "  Content : " + ContentType + " (" + _contentLength + " bytes)" + Environment.NewLine;
      if (Headers != null && Headers.Count > 0)
      {
        ret += "  Headers : " + Environment.NewLine;
        foreach (var curr in Headers) ret += "    " + curr.Key + ": " + curr.Value + Environment.NewLine;
      }
      else
      {
        ret += "  Headers : none" + Environment.NewLine;
      }

      if (Data != null)
      {
        ret += "  Data    : " + Environment.NewLine;
        switch (Data)
        {
          case byte[] bytes:
            ret += Encoding.UTF8.GetString(bytes) + Environment.NewLine;
            break;
          case string str:
            ret += str + Environment.NewLine;
            break;
          default:
            ret += TfresCommon.SerializeJson(Data) + Environment.NewLine;
            break;
        }
      }
      else
      {
        ret += "  Data    : [null]" + Environment.NewLine;
      }

      return ret;
    }

    #endregion

    //
    //
    // Do not serialize this object directly when sending a response.  Use the .ToJson() method instead
    // since the JSON output will not match in terms of actual class member names and such.
    //
    //

    #region Public-Members

    //
    // Values from the request
    //

    /// <summary>
    ///   UTC timestamp from when the response was generated.
    /// </summary>
    private readonly DateTime _timestampUtc;

    /// <summary>
    ///   IP address of the requestor (client).
    /// </summary>
    private readonly string _sourceIp;

    /// <summary>
    ///   TCP port from which the request originated on the requestor (client).
    /// </summary>
    private readonly int _sourcePort;

    /// <summary>
    ///   IP address of the recipient (server).
    /// </summary>
    private readonly string _destIp;

    /// <summary>
    ///   TCP port on which the request was received by the recipient (server).
    /// </summary>
    private readonly int _destPort;

    /// <summary>
    ///   The HTTP verb used in the request.
    /// </summary>
    private readonly string _method;

    /// <summary>
    ///   The raw (relative) URL without the querystring attached.
    /// </summary>
    private readonly string _rawUrlWithoutQuery;

    //
    // Response values
    //

    /// <summary>
    ///   The HTTP status code to return to the requestor (client).
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    ///   Indicates whether or not the request was successful, which populates the 'success' flag in the JSON response.
    /// </summary>
    private readonly bool _success;

    /// <summary>
    ///   User-supplied headers to include in the response.
    /// </summary>
    public Dictionary<string, string> Headers { get; }

    /// <summary>
    ///   User-supplied content-type to include in the response.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    ///   The length of the supplied response data.
    /// </summary>
    private readonly long _contentLength;

    /// <summary>
    ///   The data to return to the requestor in the response body.  This must be either a byte[] or string.
    /// </summary>
    public object Data { get; }

    #endregion

    #region Private-Members

    #endregion

    #region Constructor

    /// <summary>
    ///   Create a new HttpResponse object.
    /// </summary>
    /// <param name="req">The HttpRequest object for which this request is being created.</param>
    /// <param name="success">Indicates whether or not the request was successful.</param>
    /// <param name="status">The HTTP status code to return to the requestor (client).</param>
    /// <param name="data">The data to return to the requestor in the response body.  This must be either a byte[] or string.</param>
    // ReSharper disable once UnusedMember.Global
    public HttpResponse(HttpRequest req, bool success, int status, object data) : this(req, success, status, null,
                                                                                       "application/json",
                                                                                       JsonConvert
                                                                                        .SerializeObject(data))
    {
    }

    /// <summary>
    ///   Create a new HttpResponse object.
    /// </summary>
    /// <param name="req">The HttpRequest object for which this request is being created.</param>
    /// <param name="success">Indicates whether or not the request was successful.</param>
    /// <param name="status">The HTTP status code to return to the requestor (client).</param>
    /// <param name="data">The data to return to the requestor in the response body.  This must be either a byte[] or string.</param>
    // ReSharper disable once UnusedMember.Global
    public HttpResponse(HttpRequest req, bool success, int status, string message) : this(req, success, status, null,
                                                                                          "text/plain", message)
    {
    }

    /// <summary>
    ///   Create a new HttpResponse object.
    /// </summary>
    /// <param name="req">The HttpRequest object for which this request is being created.</param>
    /// <param name="success">Indicates whether or not the request was successful.</param>
    /// <param name="status">The HTTP status code to return to the requestor (client).</param>
    /// <param name="headers">User-supplied headers to include in the response.</param>
    /// <param name="contentType">User-supplied content-type to include in the response.</param>
    /// <param name="data">The data to return to the requestor in the response body.  This must be either a byte[] or string.</param>
    // ReSharper disable once UnusedMember.Global
    public HttpResponse(HttpRequest req, bool success, int status, Dictionary<string, string> headers,
                        string contentType, object data) : this(req, success, status, headers, contentType,
                                                                JsonConvert.SerializeObject(data))
    {
    }

    /// <summary>
    ///   Create a new HttpResponse object. Response is a StatusCode only.
    /// </summary>
    /// <param name="req">The HttpRequest object for which this request is being created.</param>
    /// <param name="success">Indicates whether or not the request was successful.</param>
    /// <param name="status">The HTTP status code to return to the requestor (client).</param>
    // ReSharper disable once UnusedMember.Global
    public HttpResponse(HttpRequest req, bool success, int status) : this(req, success, status, null, null, null)
    {
    }

    /// <summary>
    ///   Create a new HttpResponse object.
    /// </summary>
    /// <param name="req">The HttpRequest object for which this request is being created.</param>
    /// <param name="success">Indicates whether or not the request was successful.</param>
    /// <param name="status">The HTTP status code to return to the requestor (client).</param>
    /// <param name="headers">User-supplied headers to include in the response.</param>
    /// <param name="contentType">User-supplied content-type to include in the response.</param>
    /// <param name="data">
    ///   The data to return to the requestor in the response body.  This must be either a byte[] or string.
    ///   Indicates whether or not the response Data should be enapsulated in a JSON object containing
    ///   standard fields including 'success'.
    /// </param>
    public HttpResponse(HttpRequest req, bool success, int status, Dictionary<string, string> headers,
                        string contentType, string data)
    {
      if (req == null) throw new ArgumentNullException(nameof(req));

      #region Set-Base-Variables

      _timestampUtc = req.TimestampUtc;
      _sourceIp = req.SourceIp;
      _sourcePort = req.SourcePort;
      _destIp = req.DestIp;
      _destPort = req.DestPort;
      _method = req.Method;
      _rawUrlWithoutQuery = req.RawUrlWithoutQuery;

      _success = success;
      Headers = headers;
      ContentType = contentType;
      if (string.IsNullOrEmpty(ContentType)) ContentType = "application/json";

      StatusCode = status;
      Data = data;

      #endregion

      #region Check-Data

      if (Data != null)
        switch (Data)
        {
          case byte[] bytes:
            _contentLength = bytes.Length;
            break;
          case string s:
            _contentLength = s.Length;
            break;
          default:
            _contentLength = TfresCommon.SerializeJson(Data).Length;
            Data = TfresCommon.SerializeJson(Data);
            break;
        }
      else
        _contentLength = 0;

      #endregion
    }

    #endregion

    #region Public-Internal-Classes

    #endregion

    #region Private-Internal-Classes

    #endregion
  }
}