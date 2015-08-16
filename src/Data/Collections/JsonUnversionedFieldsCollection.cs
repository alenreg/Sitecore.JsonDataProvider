namespace Sitecore.Data.Collections
{
  using System.Collections.Generic;

  using Sitecore.Diagnostics;
  using Sitecore.Globalization;

  public class JsonUnversionedFieldsCollection : Dictionary<string, JsonFieldsCollection>
  {
    [NotNull]
    public new JsonFieldsCollection this[Language language]
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
    public new JsonFieldsCollection this[string language]
    {
      get
      {
        Assert.ArgumentNotNullOrEmpty(language, "language");

        JsonFieldsCollection value;
        if (this.TryGetValue(language, out value))
        {
          Assert.IsNotNull(value, "value");

          return value;
        }

        value = new JsonFieldsCollection();
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