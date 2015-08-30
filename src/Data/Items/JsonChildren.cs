namespace Sitecore.Data.Items
{
  using System.Collections.Generic;

  using Newtonsoft.Json;

  using Sitecore.Data.Converters;
  using Sitecore.Diagnostics;

  [JsonConverter(typeof(JsonChildrenConverter))]
  public class JsonChildren : List<JsonItem>
  {
    public JsonChildren()
    {
    }

    public JsonChildren([NotNull] IEnumerable<JsonItem> collection) : base(collection)
    {
      Assert.ArgumentNotNull(collection, nameof(collection));
    }
  }
}