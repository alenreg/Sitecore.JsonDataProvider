namespace Sitecore.Data.Collections
{
  using System.Collections.Generic;

  using Newtonsoft.Json;

  using Sitecore.Data;
  using Sitecore.Data.Converters;
  using Sitecore.Diagnostics;

  [JsonConverter(typeof(JsonFieldsCollectionConverter))]
  public class JsonFieldsCollection : Dictionary<ID, string>
  {
    public JsonFieldsCollection()
    {
    }

    public JsonFieldsCollection([NotNull] IDictionary<ID, string> dictionary)
      : base(dictionary)
    {
      Assert.ArgumentNotNull(dictionary, nameof(dictionary));
    }

    [CanBeNull]
    public new string this[ID id]
    {
      get
      {
        Assert.ArgumentNotNull(id, nameof(id));

        string value;
        if (this.TryGetValue(id, out value))
        {
          Assert.IsNotNull(value, "value");
        }

        return value;
      }

      set
      {
        Assert.ArgumentNotNull(id, nameof(id));
        Assert.ArgumentNotNull(value, nameof(value));

        base[id] = value;
      }
    }
  }
}
