namespace Sitecore.Data.Mappings
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Xml;

  using Sitecore.Data;
  using Sitecore.Data.Helpers;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;

  public class ItemChildrenMapping : AbstractMapping
  {
    [NotNull]
    public readonly ID ItemID;


    [UsedImplicitly]
    public ItemChildrenMapping([NotNull] XmlElement mappingElement, [NotNull] string databaseName)
      : base(mappingElement, databaseName)
    {
      Assert.ArgumentNotNull(mappingElement, nameof(mappingElement));
      Assert.ArgumentNotNull(databaseName, nameof(databaseName));

      var itemString = mappingElement.GetAttribute("item");
      Assert.IsNotNull(itemString, $"The \"item\" attribute is not specified or has empty string value: {mappingElement.OuterXml}");

      ID itemID;
      ID.TryParse(itemString, out itemID);
      Assert.IsNotNull(itemID, $"the \"item\" attribute is not a valid GUID value: {mappingElement.OuterXml}");

      this.ItemID = itemID;
    }

    [NotNull]
    protected override IEnumerable<JsonItem> Initialize([NotNull] string json)
    {
      Assert.ArgumentNotNull(json, nameof(json));

      var children = JsonHelper.Deserialize<List<JsonItem>>(json);
      if (children == null)
      {
        return new List<JsonItem>();
      }

      foreach (var item in children)
      {
        item.ParentID = this.ItemID;
        this.InitializeItemTree(item);
      }

      return children;
    }

    public override IEnumerable<ID> GetChildIDs(ID itemId)
    {
      Assert.ArgumentNotNull(itemId, nameof(itemId));

      if (itemId == this.ItemID)
      {
        return this.ItemChildren.Select(x => x.ID);
      }

      var item = this.GetItem(itemId);
      return item?.Children.Select(x => x.ID);
    }

    public override bool AcceptsNewChildrenOf(ID itemID)
    {
      return !ReadOnly && (itemID == this.ItemID || this.ItemsCache.Any(x => x.ID == itemID));
    }

    protected override bool IgnoreItem(JsonItem item)
    {
      return false;
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

      JsonItem parent = null;
      if (this.ItemID != parentID)
      {
        parent = this.ItemsCache.FirstOrDefault(x => x.ID == parentID);
        if (parent == null)
        {
          return false;
        }
      }

      var item = new JsonItem(itemID, parentID)
      {
        Name = itemName,
        TemplateID = templateID
      };

      lock (this.SyncRoot)
      {
        this.ItemsCache.Add(item);

        if (parent != null)
        {
          parent.Children.Add(item);
        }
        else
        {
          this.ItemChildren.Add(item);
        }

        this.Commit();
      }

      return true;
    }

    public override bool CopyItem(ID sourceItemID, ID destinationItemID, ID copyID, string copyName)
    {
      Assert.ArgumentNotNull(sourceItemID, nameof(sourceItemID));
      Assert.ArgumentNotNull(destinationItemID, nameof(destinationItemID));
      Assert.ArgumentNotNull(copyID, nameof(copyID));
      Assert.ArgumentNotNull(copyName, nameof(copyName));

      if (this.ReadOnly)
      {
        return false;
      }

      var sourceItem = this.GetItem(sourceItemID);
      if (sourceItem == null)
      {
        return false;
      }

      if (destinationItemID != this.ItemID)
      {
        var destinationItem = this.GetItem(destinationItemID);
        if (destinationItem == null)
        {
          return false;
        }
      }

      return this.DoCopyItem(destinationItemID, copyID, copyName, sourceItem);
    }

    public override bool MoveItem(ID itemID, ID targetID)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(targetID, nameof(targetID));

      if (this.ReadOnly)
      {
        return false;
      }

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return false;
      }

      var parentID = item.ParentID;
      if (parentID == targetID)
      {
        return true;
      }

      lock (this.SyncRoot)
      {
        if (parentID == this.ItemID)
        {
          var target = this.GetItem(targetID);
          Assert.IsNotNull(target, $"Moving item outside of ItemChildrenMapping ({this.ItemID}, {this.FileMappingPath}) is not supported");

          this.ItemChildren.Remove(item);
          target.Children.Add(item);
        }
        else if (targetID == this.ItemID)
        {
          var parent = this.GetItem(parentID);
          Assert.IsNotNull(parent, $"Cannot find {parentID} item");

          parent.Children.Remove(item);
          this.ItemChildren.Add(item);
        }
        else
        {
          var parent = this.GetItem(parentID);
          Assert.IsNotNull(parent, $"Cannot find {parentID} item");

          var target = this.GetItem(targetID);
          Assert.IsNotNull(targetID, $"Moving item outside of ItemChildrenMapping ({this.ItemID}, {this.FileMappingPath}) is not supported");

          parent.Children.Remove(item);
          target.Children.Add(item);
        }

        this.Commit();
      }

      return true;
    }

    protected override void DoDeleteItem(JsonItem item)
    {
      Assert.ArgumentNotNull(item, nameof(item));

      if (item.ParentID == this.ItemID)
      {
        this.ItemChildren.Remove(item);
      }
      else
      {
        var parentID = item.ParentID;
        var parent = this.GetItem(parentID);
        Assert.IsNotNull(parent, "parent");

        parent.Children.Remove(item);
      }
    }

    protected override object GetCommitObject()
    {
      return this.ItemChildren;
    }
  }
}