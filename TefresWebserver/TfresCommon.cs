using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Tfres
{
  /// <summary>
  ///   Commonly used static methods.
  /// </summary>
  public class TfresCommon
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
    ///   Deserialize JSON string to an object using Newtonsoft JSON.NET.
    /// </summary>
    /// <typeparam name="T">The type of object.</typeparam>
    /// <param name="json">JSON string.</param>
    /// <returns>An object of the specified type.</returns>
    public static T DeserializeJson<T>(string json)
    {
      if (string.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));

      try
      {
        return JsonConvert.DeserializeObject<T>(json);
      }
      catch (Exception e)
      {
        Console.WriteLine("");
        Console.WriteLine("Exception while deserializing:");
        Console.WriteLine(json);
        Console.WriteLine("");
        throw e;
      }
    }

    /// <summary>
    ///   Deserialize JSON string to an object using Newtonsoft JSON.NET.
    /// </summary>
    /// <typeparam name="T">The type of object.</typeparam>
    /// <param name="data">Byte array containing the JSON string.</param>
    /// <returns>An object of the specified type.</returns>
    public static T DeserializeJson<T>(byte[] data)
    {
      if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
      return DeserializeJson<T>(Encoding.UTF8.GetString(data));
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
    ///   Calculate the number of milliseconds between now and a supplied start time.
    /// </summary>
    /// <param name="start">The start time.</param>
    /// <returns>The number of milliseconds.</returns>
    public static double TotalMsFrom(DateTime start)
    {
      var end = DateTime.Now.ToUniversalTime();
      var total = end - start;
      return total.TotalMilliseconds;
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

    /// <summary>
    ///   Compare two URLs to see if they are equal to one another.
    /// </summary>
    /// <param name="url1">The first URL.</param>
    /// <param name="url2">The second URL.</param>
    /// <param name="includeIntegers">Indicate whether or not integers found in the URL should be included in the comparison.</param>
    /// <returns>A Boolean indicating whether or not the URLs match.</returns>
    public static bool UrlEqual(string url1, string url2, bool includeIntegers)
    {
      /* 
       * 
       * Takes two URLs as input and tokenizes.  Token demarcation characters
       * are question mark ?, slash /, ampersand &, and colon :.
       * 
       * Integers are allowed as tokens if include_integers is set to true.
       * 
       * Tokens are whitespace-trimmed and converted to lowercase.
       * 
       * At the end, the token list for each URL is compared.
       * 
       * Returns TRUE if contents same
       * Returns FALSE otherwise
       * 
       */

      if (string.IsNullOrEmpty(url1)) throw new ArgumentNullException(nameof(url1));
      if (string.IsNullOrEmpty(url2)) throw new ArgumentNullException(nameof(url2));

      var currString = "";
      int currStringInt;
      var url1Tokens = new List<string>();
      var url2Tokens = new List<string>();
      string[] url1TokensArray;
      string[] url2TokensArray;

      #region Build-Token-Lists

      #region url1

      #region Iterate

      for (var i = 0; i < url1.Length; i++)
      {
        #region Slash-or-Colon

        if (url1[i] == '/' // slash
         || url1[i] == ':') // colon
        {
          if (string.IsNullOrEmpty(currString))
          {
            #region Nothing-to-Add

            continue;

            #endregion
          }

          #region Something-to-Add

          currStringInt = 0;
          if (int.TryParse(currString, out currStringInt))
          {
            #region Integer

            if (includeIntegers) url1Tokens.Add(string.Copy(currString.ToLower().Trim()));

            currString = "";
            continue;

            #endregion
          }

          #region Not-an-Integer

          url1Tokens.Add(string.Copy(currString.ToLower().Trim()));
          currString = "";
          continue;

          #endregion

          #endregion
        }

        #endregion

        #region Question-or-Ampersand

        if (url1[i] == '?' // question
         || url1[i] == '&') // ampersand
        {
          if (string.IsNullOrEmpty(currString))
          {
            #region Nothing-to-Add

            break;

            #endregion
          }

          #region Something-to-Add

          currStringInt = 0;
          if (int.TryParse(currString, out currStringInt))
          {
            #region Integer

            if (includeIntegers) url1Tokens.Add(string.Copy(currString.ToLower().Trim()));

            currString = "";
            break;

            #endregion
          }

          #region Not-an-Integer

          url1Tokens.Add(string.Copy(currString.ToLower().Trim()));
          currString = "";
          break;

          #endregion

          #endregion
        }

        #endregion

        #region Add-Characters

        currString += url1[i];

        #endregion
      }

      #endregion

      #region Remainder

      if (!string.IsNullOrEmpty(currString))
      {
        #region Something-to-Add

        currStringInt = 0;
        if (int.TryParse(currString, out currStringInt))
        {
          #region Integer

          if (includeIntegers) url1Tokens.Add(string.Copy(currString.ToLower().Trim()));

          currString = "";

          #endregion
        }
        else
        {
          #region Not-an-Integer

          url1Tokens.Add(string.Copy(currString.ToLower().Trim()));
          currString = "";

          #endregion
        }

        #endregion
      }

      #endregion

      #region Convert-and-Enumerate

      url1TokensArray = url1Tokens.ToArray();

      #endregion

      #endregion

      #region url2

      #region Iterate

      for (var i = 0; i < url2.Length; i++)
      {
        #region Slash-or-Colon

        if (url2[i] == '/' // slash
         || url2[i] == ':') // colon
        {
          if (string.IsNullOrEmpty(currString))
          {
            #region Nothing-to-Add

            continue;

            #endregion
          }

          #region Something-to-Add

          currStringInt = 0;
          if (int.TryParse(currString, out currStringInt))
          {
            #region Integer

            if (includeIntegers) url2Tokens.Add(string.Copy(currString.ToLower().Trim()));

            currString = "";
            continue;

            #endregion
          }

          #region Not-an-Integer

          url2Tokens.Add(string.Copy(currString.ToLower().Trim()));
          currString = "";
          continue;

          #endregion

          #endregion
        }

        #endregion

        #region Question-or-Ampersand

        if (url2[i] == '?' // question
         || url2[i] == '&') // ampersand
        {
          if (string.IsNullOrEmpty(currString))
          {
            #region Nothing-to-Add

            break;

            #endregion
          }

          #region Something-to-Add

          currStringInt = 0;
          if (int.TryParse(currString, out currStringInt))
          {
            #region Integer

            if (includeIntegers) url2Tokens.Add(string.Copy(currString.ToLower().Trim()));

            currString = "";
            break;

            #endregion
          }

          #region Not-an-Integer

          url2Tokens.Add(string.Copy(currString.ToLower().Trim()));
          currString = "";
          break;

          #endregion

          #endregion
        }

        #endregion

        #region Add-Characters

        currString += url2[i];

        #endregion
      }

      #endregion

      #region Remainder

      if (!string.IsNullOrEmpty(currString))
      {
        #region Something-to-Add

        currStringInt = 0;
        if (int.TryParse(currString, out currStringInt))
        {
          #region Integer

          if (includeIntegers) url2Tokens.Add(string.Copy(currString.ToLower().Trim()));

          currString = "";

          #endregion
        }
        else
        {
          #region Not-an-Integer

          url2Tokens.Add(string.Copy(currString.ToLower().Trim()));
          currString = "";

          #endregion
        }

        #endregion
      }

      #endregion

      #region Convert-and-Enumerate

      url2TokensArray = url2Tokens.ToArray();

      #endregion

      #endregion

      #endregion

      #region Compare-and-Return

      if (url1Tokens       == null) return false;
      if (url2Tokens       == null) return false;
      if (url1Tokens.Count != url2Tokens.Count) return false;

      for (var i = 0; i < url1Tokens.Count; i++)
        if (string.Compare(url1TokensArray[i], url2TokensArray[i]) != 0)
          return false;

      return true;

      #endregion
    }

    /// <summary>
    ///   Calculate the MD5 hash of a given byte array.
    /// </summary>
    /// <param name="data">The input byte array.</param>
    /// <returns>A string containing the MD5 hash.</returns>
    public static string CalculateMd5(byte[] data)
    {
      if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
      var md5 = MD5.Create();
      var hash = md5.ComputeHash(data);
      var sb = new StringBuilder();
      for (var i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("X2"));
      return sb.ToString();
    }

    /// <summary>
    ///   Calculate the MD5 hash of a given string.
    /// </summary>
    /// <param name="data">The input string.</param>
    /// <returns>A string containing the MD5 hash.</returns>
    public static string CalculateMd5(string data)
    {
      if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(nameof(data));
      return CalculateMd5(Encoding.UTF8.GetBytes(data));
    }

    /// <summary>
    ///   Display a console prompt and return a Boolean.
    /// </summary>
    /// <param name="question">Prompt to display.</param>
    /// <param name="yesDefault">Specify whether yes/true is the default response.</param>
    /// <returns>Boolean.</returns>
    public static bool InputBoolean(string question, bool yesDefault)
    {
      Console.Write(question);

      if (yesDefault) Console.Write(" [Y/n]? ");
      else Console.Write(" [y/N]? ");

      var userInput = Console.ReadLine();

      if (string.IsNullOrEmpty(userInput))
      {
        if (yesDefault) return true;
        return false;
      }

      userInput = userInput.ToLower();

      if (yesDefault)
      {
        if (
          string.Compare(userInput, "n")  == 0
       || string.Compare(userInput, "no") == 0
        )
          return false;

        return true;
      }

      if (
        string.Compare(userInput, "y")   == 0
     || string.Compare(userInput, "yes") == 0
      )
        return true;

      return false;
    }

    /// <summary>
    ///   Display a console prompt and return a string.
    /// </summary>
    /// <param name="question">Prompt to display.</param>
    /// <param name="defaultAnswer">Specify the default value to return if no value provided by the user.</param>
    /// <param name="allowNull">True if null responses are allowed.</param>
    /// <returns>String.</returns>
    public static string InputString(string question, string defaultAnswer, bool allowNull)
    {
      while (true)
      {
        Console.Write(question);

        if (!string.IsNullOrEmpty(defaultAnswer)) Console.Write(" [" + defaultAnswer + "]");

        Console.Write(" ");

        var userInput = Console.ReadLine();

        if (string.IsNullOrEmpty(userInput))
        {
          if (!string.IsNullOrEmpty(defaultAnswer)) return defaultAnswer;
          if (allowNull) return null;
          continue;
        }

        return userInput;
      }
    }

    /// <summary>
    ///   Display a console prompt and return an integer.
    /// </summary>
    /// <param name="question">Prompt to display.</param>
    /// <param name="defaultAnswer">Specify the default value to return if no value provided by the user.</param>
    /// <param name="positiveOnly">True if only positive numbers can be supplied.</param>
    /// <param name="allowZero">True if zero is an accepted value.</param>
    /// <returns>Integer.</returns>
    public static int InputInteger(string question, int defaultAnswer, bool positiveOnly, bool allowZero)
    {
      while (true)
      {
        Console.Write(question);
        Console.Write(" [" + defaultAnswer + "] ");

        var userInput = Console.ReadLine();

        if (string.IsNullOrEmpty(userInput)) return defaultAnswer;

        var ret = 0;
        if (!int.TryParse(userInput, out ret))
        {
          Console.WriteLine("Please enter a valid integer.");
          continue;
        }

        if (ret == 0)
          if (allowZero)
            return 0;

        if (ret < 0)
          if (positiveOnly)
          {
            Console.WriteLine("Please enter a value greater than zero.");
            continue;
          }

        return ret;
      }
    }

    #endregion

    #region Private-Methods

    #endregion
  }
}