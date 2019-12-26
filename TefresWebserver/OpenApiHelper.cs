#region

using System.Globalization;
using System.IO;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

#endregion

namespace Tfres
{
  public static class OpenApiHelper
  {
    public static string ConvertToJson(this OpenApiDocument document)
    {
      var outputStringWriter = new StringWriter(CultureInfo.InvariantCulture);
      var writer = new OpenApiJsonWriter(outputStringWriter);
      document.SerializeAsV3(writer);
      writer.Flush();
      return outputStringWriter.GetStringBuilder().ToString();
    }
  }
}