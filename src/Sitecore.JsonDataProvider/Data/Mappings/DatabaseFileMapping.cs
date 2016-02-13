namespace Sitecore.Data.Mappings
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Xml;

  using Sitecore.Data;
  using Sitecore.Data.DataProviders;
  using Sitecore.Data.Helpers;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;

  public class DatabaseFileMapping : AbstractFileMapping
  {
    [UsedImplicitly]
    public DatabaseFileMapping([NotNull] XmlElement mappingElement, [NotNull] string databaseName)
      : base(mappingElement, databaseName)
    {
      Assert.ArgumentNotNull(mappingElement, nameof(mappingElement));
      Assert.ArgumentNotNull(databaseName, nameof(databaseName));
    }

    protected override IEnumerable<JsonItem> Initialize(string json)
    {
      Assert.ArgumentNotNull(json, nameof(json));

      var dictionary = JsonHelper.Deserialize<IDictionary<string, List<JsonItem>>>(json);
      if (dictionary == null)
      {
        return new List<JsonItem>();
      }

      var children = dictionary.Select(x => new JsonItem(ID.Parse(x.Key), ID.Null, new JsonChildren(x.Value))).ToList();

      foreach (var item in children)
      {
        item.ParentID = ID.Null;

        this.InitializeItemTree(item);
      }

      return children;
    }

    public override IEnumerable<ID> GetChildIDs(ID itemId)
    {
      Assert.ArgumentNotNull(itemId, nameof(itemId));

      var item = this.GetItem(itemId);

      // no need to check IgnoreItem(item)
      return item?.Children.Select(x => x.ID);
    }

    public override bool AcceptsNewChildrenOf([CanBeNull] ID itemID)
    {
      return !this.ReadOnly;
    }

    protected override bool IgnoreItem(JsonItem item)
    {
      return item.ParentID == ID.Null;
    }

    public override bool CreateItem(ID itemID, string itemName, ID templateID, ID parentID)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(itemName, nameof(itemName));
      Assert.ArgumentNotNull(templateID, nameof(templateID));
      Assert.ArgumentNotNull(parentID, nameof(parentID));

      if (this.ReadOnly)
      {
        return false;
      }

      var parent = this.GetItem(parentID);

      Lock.EnterWriteLock();
      try
      {
        // no need to check: parent.ParentID == ID.Null
        if (parent == null)
        {
          parent = this.AddRootItem(parentID);
        }

        var item = new JsonItem(itemID, parentID)
        {
          Name = itemName,
          TemplateID = templateID
        };

        this.ItemsCache[item.ID] = item;

        parent.Children.Add(item);
      }
      finally
      {
        Lock.ExitWriteLock();
      }

      this.Commit();

      return true;
    }

    public override bool CopyItem(ID sourceItemID, ID destinationItemID, ID copyID, string copyName, CallContext context)
    {
      Assert.ArgumentNotNull(sourceItemID, nameof(sourceItemID));
      Assert.ArgumentNotNull(destinationItemID, nameof(destinationItemID));
      Assert.ArgumentNotNull(copyID, nameof(copyID));
      Assert.ArgumentNotNull(copyName, nameof(copyName));

      if (this.ReadOnly)
      {
        return false;
      }

      var destinationItem = this.GetItem(destinationItemID);

      Lock.EnterWriteLock();
      try
      {
        // no need to check: destinationItem.ParentID == ID.Null
        if (destinationItem == null)
        {
          destinationItem = AddRootItem(destinationItemID);
        }

        var item = DoCopy(sourceItemID, destinationItemID, copyID, copyName, context);

        this.ItemsCache[item.ID] = item;

        destinationItem.Children.Add(item);
      }
      finally
      {
        Lock.ExitWriteLock();
      }

      this.Commit();

      return true;
    }

    public override bool MoveItem(ID itemID, ID targetID)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(targetID, nameof(targetID));

      var item = this.GetItem(itemID);
      if (item == null || this.IgnoreItem(item))
      {
        return false;
      }

      var target = this.GetItem(targetID);
      if (target == null)
      {
        target = this.AddRootItem(targetID);
      }

      var parent = this.GetItem(item.ParentID);
      Assert.IsNotNull(parent, "Cannot find {0} item", item.ParentID);
      Assert.IsTrue(!this.IgnoreItem(parent), "!this.IgnoreItem(parent)");

      if (this.ReadOnly)
      {
        return false;
      }

      Lock.EnterWriteLock();
      try
      {
        parent.Children.Remove(item);
        target.Children.Add(item);
      }
      finally
      {
        Lock.ExitWriteLock();
      }

      this.Commit();

      return true;
    }

    protected override void DoDeleteItem(JsonItem item)
    {
      Assert.ArgumentNotNull(item, nameof(item));

      var parentID = item.ParentID;
      var parent = this.ItemsCache[parentID];
      Assert.IsNotNull(parent, "parent");

      parent.Children.Remove(item);
    }

    protected override object GetCommitObject()
    {
      // no need to lock
      return this.ItemChildren.Where(x => x.Children.Count > 0).ToDictionary(x => x.ID.ToString(), x => x.Children);
    }

    [NotNull]
    private JsonItem AddRootItem([NotNull] ID itemID)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));

      var rootItem = new JsonItem(itemID, ID.Null)
      {
        Name = "$default-mapping"
      };

      // no need to lock 
      this.ItemChildren.Add(rootItem);
      this.ItemsCache[rootItem.ID] = rootItem;

      return rootItem;
    }
  }
}