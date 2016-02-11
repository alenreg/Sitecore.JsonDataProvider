using System.Collections.Generic;
using Sitecore.Diagnostics;

namespace Sitecore.Data.Collections
{
  public abstract class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue>
  {
    protected DefaultDictionary()
    {
    }

    protected DefaultDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
    {
    }

    [NotNull]
    public new TValue this[TKey key]
    {
      get
      {
        Assert.ArgumentNotNull(key, nameof(key));

        TValue value;
        if (this.TryGetValue(key, out value))
        {
          Assert.IsNotNull(value, "value");

          return value;
        }

        var defaultValue = GetDefaultValue(key);
        Assert.IsNotNull(defaultValue, "defaultValue");

        base[key] = defaultValue;

        return defaultValue;
      }

      set
      {
        Assert.ArgumentNotNull(key, nameof(key));
        Assert.ArgumentNotNull(value, nameof(value));

        base[key] = value;
      }
    }

    [NotNull]
    protected abstract TValue GetDefaultValue(TKey key);
  }
}