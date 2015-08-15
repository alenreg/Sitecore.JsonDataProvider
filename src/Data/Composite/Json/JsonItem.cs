namespace Sitecore.Support.Data.Composite.Json
{
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;

  using Newtonsoft.Json;

  using Sitecore.Data;
  using Sitecore.Data.DataProviders;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.StringExtensions;
  using Sitecore.Support.Data.DataProviders;

  public class JsonItem : CompositeItem
  {
    [CanBeNull]
    private List<JsonItem> children;

    [JsonIgnore]
    private List<JsonLanguage> languageVersions;

    private ID id;

    [CanBeNull]
    private ID templateID;

    [CanBeNull]
    public string Name { get; set; }
    
    [CanBeNull]
    public ID Id { get; set; }
    
    [CanBeNull]
    public List<JsonLanguage> LanguageVersions
    {
      get
      {
        return this.languageVersions;
      }
      set
      {
        this.languageVersions = value;
      }
    }

    [CanBeNull]
    public ID TemplateId { get; set; }

    public override ID ID
    {
      get
      {
        var value = this.id;
        Assert.IsNotNull(value, "value");

        return value;
      }
    }

    public override ID TemplateID
    {
      get
      {
        var value = this.templateID;
        Assert.IsNotNull(value, "value");

        return value;
      }
    }

    public override ID ParentID
    {
      get
      {
      ???
      }
    }

    [CanBeNull, JsonIgnore]
    public JsonItem Parent { get; set; }

    public override IReadOnlyCollection<CompositeItem> Children
    {
      get
      {
      ???
      }
    }

    public override IReadOnlyCollection<CompositeItemLanguage> Languages
    {
      get
      {
      ???
      }
    }

    public override CompositeItem AddChild(ID itemID, string itemName, CallContext context)
    {
    ???
    }

    public override CompositeItemLanguage AddLanguage(string languageName)
    {
    ???
    }

    public void Validate(bool deep)
    {
      var id = this.Id as object;
      if (id == null)
      {
        throw new InvalidDataException("The item does not have an ID");
      }

      var name = this.Name;
      if (string.IsNullOrEmpty(name))
      {
        throw new InvalidDataException("The {0} item does not have a name".FormatWith(id));
      }

      var templateId = this.TemplateId as object;
      if (templateId == null)
      {
        throw new InvalidDataException("The {0} item does not have a template ID".FormatWith(id));
      }

      if (!deep)
      {
        return;
      }

      var children = this.Children;
      foreach (var child in children)
      {
        if (child == null)
        {
          throw new InvalidDataException("The {0} item contains null in Children collection");
        }
        
        child.Validate(true);
      }
    }

    [CanBeNull]
    public JsonVersion AddVersion([NotNull] Language language, int number)
    {
      Assert.ArgumentNotNull(language, "language");

      var languageVersion = this.GetVersion(language);
      if (languageVersion != null)
      {
        return languageVersion.AddVersion(number);
      }

      lock (this.LanguageVersions)
      {
        languageVersion = this.DoAddVersion(language);
      }

      return languageVersion.AddVersion(number);
    }

    public void Initialize([CanBeNull] JsonItem parent = null)
    {
      if (parent != null)
      {
        this.Parent = parent;
      }

      foreach (var languageVersion in this.LanguageVersions)
      {
        languageVersion.Initialize(this);
      }

      var children = this.Children;
      Assert.IsNotNull(children, "children");

      foreach (var child in children)
      {
        child.Initialize(this);
      }
    }

    [NotNull]
    private JsonLanguage DoAddVersion([NotNull] Language language)
    {
      Assert.ArgumentNotNull(language, "language");

      var version = new JsonLanguage { Language = language.Name };
      var versions = this.LanguageVersions;
      lock (versions)
      {
        versions.Add(version);
      }

      return version;
    }

    [CanBeNull]
    private JsonLanguage GetVersion([NotNull] Language language)
    {
      Assert.ArgumentNotNull(language, "language");

      return this.LanguageVersions.FirstOrDefault(x => x.Language == language.Name);
    }
  }
}