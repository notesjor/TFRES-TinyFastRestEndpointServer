using Newtonsoft.Json;

namespace Tfres.Documentation
{
  public class SericeDocumentation
  {
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("endpoints")]
    public ServiceEndpoint[] Endpoints { get; set; }
  }
}
