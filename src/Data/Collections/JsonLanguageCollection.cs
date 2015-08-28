namespace Sitecore.Data.Collections
{
  using System.Collections.Generic;

  using Newtonsoft.Json;

  using Sitecore.Data.Converters;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;

  [JsonConverter(typeof(JsonLanguageCollectionConverter))]
  public class JsonLanguageCollection : Dictionary<string, JsonVersionCollection>
  {
    [NotNull]
    public JsonVersionCollection this[Language language]
    {
      get
      {
        Assert.ArgumentNotNull(language, "language");

        return this[language.Name];
      }

      set
      {
        Assert.ArgumentNotNull(language, "language");
        Assert.ArgumentNotNull(value, "value");

        this[language.Name] = value;
      }
    }

    [NotNull]
    public new JsonVersionCollection this[string language]
    {
      get
      {
        Assert.ArgumentNotNullOrEmpty(language, "language");

        JsonVersionCollection value;
        if (this.TryGetValue(language, out value))
        {
          Assert.IsNotNull(value, "value");

          return value;
        }

        value = new JsonVersionCollection();
        return base[language] = value;
      }

      set
      {
        Assert.ArgumentNotNullOrEmpty(language, "language");
        Assert.ArgumentNotNull(value, "value");

        base[language] = value;
      }
    }
  }
}
