namespace Sitecore.Support.Data.Collections
{
  using System.Collections.Generic;

  using Sitecore.Diagnostics;

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
