namespace Sitecore.Data.Collections
{
  using System.Collections.Generic;

  using Newtonsoft.Json;

  using Sitecore.Data.Converters;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;

  [JsonConverter(typeof(JsonUnversionedFieldsCollectionConverter))]
  public class JsonUnversionedFieldsCollection : Dictionary<string, JsonFieldsCollection>
  {
    [NotNull]
    public JsonFieldsCollection this[Language language]
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

    public void RemoveField([NotNull] ID fieldID)
    {
      Assert.ArgumentNotNull(fieldID, "fieldID");

      foreach (var languageFields in this.Values)
      {
        if (languageFields == null)
        {
          continue;
        }

        languageFields.Remove(fieldID);
      }
    }
  }
}