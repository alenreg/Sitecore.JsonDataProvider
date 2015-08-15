namespace Sitecore.Support.Data.DataProviders
{
  using System.Collections.Generic;

  using Sitecore.Data;
  using Sitecore.Data.DataProviders;

  public abstract class CompositeFile
  {
    [NotNull]
    public abstract ID ParentID { get; }

    [NotNull]
    public abstract IReadOnlyCollection<CompositeItem> RootItems { get; }

    [CanBeNull]
    public abstract CompositeItem FindItem([NotNull] ID itemId);

    public abstract void CreateRootItem([NotNull] ID itemID, [NotNull] string itemName, [NotNull] ID templateID, [NotNull] CallContext context);

    public abstract void CreateChildItem([NotNull] ID itemID, [NotNull] string itemName, [NotNull] CompositeItem parentItem, [NotNull] CallContext context);

    public void Commit()
    {
      lock (this)
      {
        this.DoCommit();
      }
    }

    protected abstract void DoCommit();
  }
}