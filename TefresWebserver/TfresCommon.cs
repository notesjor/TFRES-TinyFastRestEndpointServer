using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Tfres
{
  /// <summary>
  ///   Commonly used static methods.
  /// </summary>
  public static class TfresCommon
  {
    #region Public-Members

    #endregion

    #region Private-Members

    #endregion

    #region Constructor

    #endregion

    #region Public-Internal-Classes

    #endregion

    #region Private-Internal-Classes

    #endregion

    #region Public-Methods

    /// <summary>
    ///   Serialize object to JSON using Newtonsoft JSON.NET.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>JSON string.</returns>
    public static string SerializeJson(object obj)
    {
      if (obj == null) return null;
      var json = JsonConvert.SerializeObject(
                                             obj,
                                             Formatting.Indented,
                                             new JsonSerializerSettings
                                             {
                                               NullValueHandling = NullValueHandling.Ignore,
                                               DateTimeZoneHandling = DateTimeZoneHandling.Utc
                                             });

      return json;
    }

    /// <summary>
    ///   Fully read a stream into a byte array.
    /// </summary>
    /// <param name="input">The input stream.</param>
    /// <returns>A byte array containing the data read from the stream.</returns>
    public static byte[] StreamToBytes(Stream input)
    {
      if (input == null) throw new ArgumentNullException(nameof(input));
      if (!input.CanRead) throw new InvalidOperationException("Input stream is not readable");

      var buffer = new byte[16 * 1024];
      using (var ms = new MemoryStream())
      {
        int read;

        while ((read = input.Read(buffer, 0, buffer.Length)) > 0) ms.Write(buffer, 0, read);

        return ms.ToArray();
      }
    }

    /// <summary>
    ///   Add a key-value pair to a supplied Dictionary.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="val">The value.</param>
    /// <param name="existing">An existing dictionary.</param>
    /// <returns>The existing dictionary with a new key and value, or, a new dictionary with the new key value pair.</returns>
    public static Dictionary<string, string> AddToDict(string key, string val, Dictionary<string, string> existing)
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
        var tempVal = existing[key];
        tempVal += "," + val;
        existing.Remove(key);
        existing.Add(key, tempVal);
        return existing;
      }

      if (existing.ContainsKey(key.ToLower()))
      {
        var tempVal = existing[key.ToLower()];
        tempVal += "," + val;
        existing.Remove(key.ToLower());
        existing.Add(key.ToLower(), tempVal);
        return existing;
      }

      existing.Add(key, val);
      return existing;
    }

    #endregion

    #region Private-Methods

    #endregion
  }
}