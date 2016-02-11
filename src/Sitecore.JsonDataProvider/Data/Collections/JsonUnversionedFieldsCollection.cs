namespace Sitecore.Data.Collections
{
  using Newtonsoft.Json;

  using Sitecore.Data.Converters;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;

  [JsonConverter(typeof(JsonUnversionedFieldsCollectionConverter))]
  public class JsonUnversionedFieldsCollection : DefaultDictionary<string, JsonFieldsCollection>
  {
    [NotNull]
    public JsonFieldsCollection this[Language language]
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

    protected override JsonFieldsCollection GetDefaultValue(string key)
    {
      return new JsonFieldsCollection();
    }

    public void RemoveField([NotNull] ID fieldID)
    {
      Assert.ArgumentNotNull(fieldID, nameof(fieldID));

      foreach (var languageFields in this.Values)
      {
        languageFields?.Remove(fieldID);
      }
    }
  }
}