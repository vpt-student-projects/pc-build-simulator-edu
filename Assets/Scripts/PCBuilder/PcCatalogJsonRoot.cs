using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Корень JSON каталога (StreamingAssets или ответ HTTP).
/// </summary>
public class PcCatalogJsonRoot
{
    [JsonProperty("components")]
    public List<PcComponentData> Components { get; set; } = new List<PcComponentData>();
}
