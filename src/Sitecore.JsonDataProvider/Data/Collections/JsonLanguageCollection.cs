namespace Sitecore.Data.Collections
{
  using System.Linq;

  using Newtonsoft.Json;

  using Sitecore.Data.Converters;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;

  [JsonConverter(typeof(JsonLanguageCollectionConverter))]
  public class JsonLanguageCollection : DefaultDictionary<string, JsonVersionCollection>
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

    protected override JsonVersionCollection GetDefaultValue(string key)
    {
      return new JsonVersionCollection();
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
