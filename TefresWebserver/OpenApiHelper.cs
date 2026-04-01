#region

using System.Globalization;
using System.IO;
using Microsoft.OpenApi;

#endregion

namespace Tfres
{
  public static class OpenApiHelper
  {
    public static string ConvertToJson(OpenApiDocument document)
    {
      var outputStringWriter = new StringWriter(CultureInfo.InvariantCulture);
      var writer = new OpenApiJsonWriter(outputStringWriter);
      document.SerializeAsV3(writer);
      writer.FlushAsync().Wait();
      return outputStringWriter.GetStringBuilder().ToString();
    }
  }
}