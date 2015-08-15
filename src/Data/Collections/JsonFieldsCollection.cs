namespace Sitecore.Support.Data.Collections
{
  using System.Collections.Generic;

  using Newtonsoft.Json;

  using Sitecore.Data;
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
      Assert.ArgumentNotNull(dictionary, "dictionary");
    }

    [CanBeNull]
    public new string this[ID id]
    {
      get
      {
        Assert.ArgumentNotNull(id, "id");

        string value;
        if (this.TryGetValue(id, out value))
        {
          Assert.IsNotNull(value, "value");
        }

        return value;
      }

      set
      {
        Assert.ArgumentNotNull(id, "id");
        Assert.ArgumentNotNull(value, "value");

        base[id] = value;
      }
    }
  }
}
