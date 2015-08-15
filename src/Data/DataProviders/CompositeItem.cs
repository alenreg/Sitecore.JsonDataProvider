namespace Sitecore.Support.Data.DataProviders
{
  using System.Collections.Generic;
  using System.Linq;

  using Sitecore.Data;
  using Sitecore.Data.DataProviders;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;

  public abstract class CompositeItem
  {
    [NotNull]
    public abstract ID ID { get; }

    [NotNull]
    public abstract ID TemplateID { get; }

    [NotNull]
    public abstract ID ParentID { get; }

    [CanBeNull]
    public abstract CompositeItem Parent { get; protected set; }

    [NotNull]
    public abstract CompositeFile File { get; }

    [NotNull]
    public abstract IReadOnlyCollection<CompositeItem> Children { get; }

    [NotNull]
    public abstract IReadOnlyCollection<CompositeItemLanguage> Languages { get; }

    [NotNull]
    public abstract CompositeItem AddChild([NotNull] ID itemID, [NotNull] string itemName, [NotNull] CallContext context);

    [NotNull]
    public abstract CompositeItemLanguage AddLanguage([NotNull] string languageName);

    [CanBeNull]
    public CompositeItemVersion AddVersion([NotNull] Language language, int number = 0)
    {
      Assert.ArgumentNotNull(language, "language");

      var languageName = language.Name;
      var itemLanguage = this.Languages.FirstOrDefault(x => x.Language == languageName);
      if (itemLanguage == null)
      {
        itemLanguage = this.AddLanguage(languageName);
      }

      return itemLanguage.AddVersion(number);
    }

    public void Initialize([NotNull] CompositeItem parent)
    {
      Assert.ArgumentNotNull(item, "item");

      this.Parent = parent;
      foreach (var version in this.Versions)
      {
        Assert.IsNotNull(version, "version");

        version.Initialize(this);
      }
    }
  }
}