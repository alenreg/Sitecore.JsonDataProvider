namespace Sitecore.Data.Items
{
  using Newtonsoft.Json;

  using Sitecore.Data.Collections;
  using Sitecore.Data.Helpers;

  [JsonConverter(typeof(JsonFieldsConverter))]
  public class JsonFields
  {
    [NotNull]
    public readonly JsonFieldsCollection Shared = new JsonFieldsCollection();

    [NotNull]
    public readonly JsonUnversionedFieldsCollection Unversioned = new JsonUnversionedFieldsCollection();

    [NotNull]
    public readonly JsonLanguageCollection Versioned = new JsonLanguageCollection();
  }
}