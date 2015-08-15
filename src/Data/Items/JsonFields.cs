namespace Sitecore.Support.Data.Items
{
  using Sitecore.Support.Data.Collections;

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