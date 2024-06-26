﻿#region

using System;
using System.Text;

#endregion

namespace Tfres
{
  public class HttpRequestFile
  {
    private readonly StringBuilder _stb = new StringBuilder();

    private static string[] _separator = { "; name=\"", "\"; filename=\"" };

    public HttpRequestFile(string h1, string h2)
    {
      /* SAMPLE:
     ------WebKitFormBoundarybCVI7Rwr2zly1O5N
     Content-Disposition: form-data; name="files"; filename="underc.jpg"
     Content-Type: image/jpeg
     */

      // en passant cleaning
      var items = h1.Replace("Content-Disposition: ", "")
                    .Split(_separator,
                           StringSplitOptions.RemoveEmptyEntries);

      ContentDisposition = items[0];
      Name = items[1];
      Filename = items[2].Substring(0, items[2].Length - 1);
      ContentType = h2.Replace("Content-Type: ", "").Trim();
    }

    public string ContentDisposition { get; set; }
    public string ContentType { get; set; }
    public byte[] Data { get; set; }
    public string Filename { get; set; }
    public string Name { get; set; }

    internal void AddLine(string line) => _stb.AppendLine(line);

    internal void Finalize(Encoding encoding)
    {
      Data = encoding.GetBytes(_stb.ToString());
      _stb.Clear();
    }
  }
}