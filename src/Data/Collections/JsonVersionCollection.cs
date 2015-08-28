namespace Sitecore.Data.Collections
{
  using System.Collections.Generic;

  using Newtonsoft.Json;

  using Sitecore.Data.Converters;
  using Sitecore.Diagnostics;

  [JsonConverter(typeof(JsonVersionCollectionConverter))]
  public class JsonVersionCollection : Dictionary<int, JsonFieldsCollection>
  { 
    [CanBeNull]
    public new JsonFieldsCollection this[int number]
    {
      get
      {
        JsonFieldsCollection value;
        if (this.TryGetValue(number, out value))
        {
          Assert.IsNotNull(value, "value");
        }

        return value;
      }

      set
      {
        Assert.ArgumentNotNull(value, "value");

        base[number] = value;
      }
    }
  }
}
