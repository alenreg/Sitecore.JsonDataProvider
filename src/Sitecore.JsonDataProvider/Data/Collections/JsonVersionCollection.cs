namespace Sitecore.Data.Collections
{
  using System.Linq;

  using Newtonsoft.Json;

  using Sitecore.Data.Converters;
  using Sitecore.Diagnostics;

  [JsonConverter(typeof(JsonVersionCollectionConverter))]
  public class JsonVersionCollection : NullDictionary<int, JsonFieldsCollection>
  { 
    [CanBeNull]
    public string GetFieldValue([NotNull] ID fieldID)
    {
      Assert.ArgumentNotNull(fieldID, nameof(fieldID));

      return this.OrderByDescending(x => x.Key).Select(x => x.Value[fieldID]).FirstOrDefault();
    }
  }
}
