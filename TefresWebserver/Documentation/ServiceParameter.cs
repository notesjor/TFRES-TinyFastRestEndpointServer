using Newtonsoft.Json;

namespace Tfres.Documentation
{
  public class ServiceParameter
  {
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
  }
}