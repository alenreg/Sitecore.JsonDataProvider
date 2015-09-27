﻿namespace Sitecore.Data.Collections
{
  using System.Collections.Generic;
  using System.Linq;

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
        Assert.ArgumentNotNull(value, nameof(value));

        base[number] = value;
      }
    }

    [CanBeNull]
    public string GetFieldValue([NotNull] ID fieldID)
    {
      Assert.ArgumentNotNull(fieldID, nameof(fieldID));

      return this.OrderByDescending(x => x.Key).Select(x => x.Value[fieldID]).FirstOrDefault();
    }
  }
}