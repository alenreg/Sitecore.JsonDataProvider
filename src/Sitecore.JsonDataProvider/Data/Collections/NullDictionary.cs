namespace Sitecore.Data.Collections
{
  using System.Collections.Generic;
  using Sitecore.Diagnostics;

  public class NullDictionary<TKey, TValue> : Dictionary<TKey, TValue>
  {
    public NullDictionary()
    {
    }

    public NullDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
    {
    }

    [CanBeNull]
    public new TValue this[TKey key]
    {
      get
      {
        Assert.ArgumentNotNull(key, nameof(key));

        TValue value;
        if (this.TryGetValue(key, out value))
        {
          Assert.IsNotNull(value, "value");
        }

        return value;
      }

      set
      {
        Assert.ArgumentNotNull(key, nameof(key));
        Assert.ArgumentNotNull(value, nameof(value));

        base[key] = value;
      }
    }
  }
}