namespace Sitecore.Support.Data.DataProviders
{
  using System.Collections.Generic;

  using Sitecore.Diagnostics;

  public abstract class CompositeItemLanguage
  {
    [NotNull]
    public abstract IReadOnlyCollection<CompositeItemVersion> Versions { get; }

    [NotNull]
    public abstract string Language { get; }

    [CanBeNull]
    public abstract CompositeItemVersion AddVersion(int number);

    [NotNull]
    public CompositeItem Item { get; private set; }

    public void Initialize([NotNull] CompositeItem item)
    {
      Assert.ArgumentNotNull(item, "item");

      this.Item = item;
      foreach (var version in this.Versions)
      {
        Assert.IsNotNull(version, "version");

        version.Initialize(this);
      }
    }
  }
}