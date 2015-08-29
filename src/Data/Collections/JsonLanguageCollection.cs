namespace Sitecore.Data.Collections
{
  using System.Collections.Generic;
  using System.Linq;

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

    public void RemoveField([NotNull] ID fieldID)
    {
      Assert.ArgumentNotNull(fieldID, "fieldID");

      foreach (var languageVersions in this.Values)
      {
        if (languageVersions == null)
        {
          continue;
        }

        foreach (var versionFields in languageVersions.Values)
        {
          if (versionFields != null)
          {
            versionFields.Remove(fieldID);
          }
        }
      }
    }

    [CanBeNull]
    public string GetFieldValue([NotNull] string language, [NotNull] ID fieldID)
    {
      Assert.ArgumentNotNull(language, "language");
      Assert.ArgumentNotNull(fieldID, "fieldID");

      if (!this.ContainsKey(language))
      {
        return null;
      }

      return this[language].OrderByDescending(x => x.Key).Select(x => x.Value[fieldID]).FirstOrDefault();
    }
  }
}
