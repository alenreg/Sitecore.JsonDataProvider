namespace Sitecore.Support.Data.Composite.Json
{
  using System.Collections.Generic;
  using System.Linq;

  using Sitecore.Diagnostics;
  using Sitecore.Support.Data.DataProviders;

  /// <summary>
  /// Represents item's unversioned fields in specific language and items' versions in this language
  /// </summary>
  public class JsonLanguage : CompositeItemLanguage
  {
    [CanBeNull]
    private List<JsonVersion> versions;

    [CanBeNull]
    private string language;
    
    public override string Language
    {
      get
      {
        var value = this.language ?? (this.language = "en");
        Assert.IsNotNull(value, "value");

        return value;
      }
    }

    public override IReadOnlyCollection<CompositeItemVersion> Versions
    {
      get
      {
        var versions = this.GetVersions();
        Assert.IsNotNull(versions, "versions");

        return versions;
      }
    }

    public override CompositeItemVersion AddVersion(int number)
    {
      // this logic was copied from SqlDataProvider implementation in 8.0 Update-3
      var versionsList = this.GetVersions();
      lock (versionsList)
      {
        var newNumber = versionsList.Count == 0 ? 1 : versionsList.Max(x => x.Number) + 1;
        if (number > 0)
        {
          var existingVersion = versionsList.SingleOrDefault(x => x.Number == number);
          if (existingVersion == null)
          {
            return null;
          }

          return existingVersion.CreateFrom(existingVersion, newNumber);
        }

        var version = new JsonVersion(newNumber);
        version.Fields[FieldIDs.Created.ToString()] = DateUtil.IsoNowWithTicks;
        versionsList.Add(version);

        return version;
      }
    }

    [NotNull]
    private List<JsonVersion> GetVersions()
    {
      var value = this.versions;
      if (value != null)
      {
        return value;
      }

      lock (this)
      {
        return this.versions = new List<JsonVersion>();
      }
    }
  }
}