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
        Assert.ArgumentNotNull(language, nameof(language));

        return this[language.Name];
      }

      set
      {
        Assert.ArgumentNotNull(language, nameof(language));
        Assert.ArgumentNotNull(value, nameof(value));

        this[language.Name] = value;
      }
    }

    [NotNull]
    public new JsonVersionCollection this[string language]
    {
      get
      {
        Assert.ArgumentNotNullOrEmpty(language, nameof(language));

        JsonVersionCollection value;
        if (this.TryGetValue(language, out value))
        {
          Assert.IsNotNull(value, "value");

          return value;
        }

        value = new JsonVersionCollection();

        this.Add(language, value);

        return value;
      }

      set
      {
        Assert.ArgumentNotNullOrEmpty(language, nameof(language));
        Assert.ArgumentNotNull(value, nameof(value));

        base[language] = value;
      }
    }

    public void RemoveField([NotNull] ID fieldID)
    {
      Assert.ArgumentNotNull(fieldID, nameof(fieldID));

      foreach (var languageVersions in this.Values)
      {
        if (languageVersions == null)
        {
          continue;
        }

        foreach (var versionFields in languageVersions.Values)
        {
          versionFields?.Remove(fieldID);
        }
      }
    }

    [CanBeNull]
    public string GetFieldValue([NotNull] string language, [NotNull] ID fieldID)
    {
      Assert.ArgumentNotNull(language, nameof(language));
      Assert.ArgumentNotNull(fieldID, nameof(fieldID));

      if (!this.ContainsKey(language))
      {
        return null;
      }

      return this[language].OrderByDescending(x => x.Key).Select(x => x.Value[fieldID]).FirstOrDefault();
    }
  }
}
