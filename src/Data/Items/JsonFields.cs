namespace Sitecore.Data.Items
{
  using System.Collections.Generic;
  using System.Linq;

  using Sitecore.Data.Collections;

  public class JsonFields
  {
    [NotNull]
    public readonly JsonFieldsCollection Shared = new JsonFieldsCollection();

    [NotNull]
    public readonly JsonUnversionedFieldsCollection Unversioned = new JsonUnversionedFieldsCollection();

    [NotNull]
    public readonly JsonLanguageCollection Versioned = new JsonLanguageCollection();

    [NotNull]
    public IEnumerable<string> GetAllLanguages()
    {
      return this.Versioned.Keys.Concat(this.Unversioned.Keys).Distinct().OrderByDescending(x => x == "en");
    }
  }
}