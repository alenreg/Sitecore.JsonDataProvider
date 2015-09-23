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

    public IEnumerable<string> GetFieldValues(ID fieldID)
    {
      var shared = this.Shared[fieldID];
      if (shared != null)
      {
        yield return shared;
      }

      foreach (var fields in Unversioned.Values)
      {
        var unversioned = fields[fieldID];
        if (unversioned != null)
        {
          yield return unversioned;
        }
      }

      foreach (var languages in this.Versioned.Values)
      {
        foreach (var fields in languages.Values)
        {
          var versioned = fields[fieldID];
          if (versioned != null)
          {
            yield return versioned;
          }
        }
      }
    }
  }
}