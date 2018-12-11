using Newtonsoft.Json;

namespace Tfres.Documentation
{
  public class ServiceEndpoint
  {
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("allowedVerbs")]
    public string[] AllowedVerbs { get; set; }

    [JsonProperty("arguments")]
    public ServiceArgument[] Arguments { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("returnValue")]
    public ServiceParameter[] ReturnValue { get; set; }
  }
}