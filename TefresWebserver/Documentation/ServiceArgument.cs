using Newtonsoft.Json;

namespace Tfres.Documentation
{
  public class ServiceArgument : ServiceParameter
  {
    [JsonProperty("isRequired")]
    public bool IsRequired { get; set; }
  }
}