namespace Sitecore.Data.Collections
{
  using System.Collections.Generic;

  using Newtonsoft.Json;

  using Sitecore.Data;
  using Sitecore.Data.Converters;
  using Sitecore.Diagnostics;

  [JsonConverter(typeof(JsonFieldsCollectionConverter))]
  public class JsonFieldsCollection : NullDictionary<ID, string>
  {
    public JsonFieldsCollection()
    {
    }

    public JsonFieldsCollection([NotNull] IDictionary<ID, string> dictionary)
      : base(dictionary)
    {
      Assert.ArgumentNotNull(dictionary, nameof(dictionary));
    }
  }
}
