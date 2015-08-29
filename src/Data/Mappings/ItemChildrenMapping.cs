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
    public ItemChildrenMapping([NotNull] XmlElement mappingElement) : base(mappingElement)
    {
      Assert.ArgumentNotNull(mappingElement, "mappingElement");

      var itemString = mappingElement.GetAttribute("item");
      Assert.IsNotNull(itemString, "The \"item\" attribute is not specified or has empty string value: " + mappingElement.OuterXml);

      ID itemID;
      ID.TryParse(itemString, out itemID);
      Assert.IsNotNull(itemID, "the \"item\" attribute is not a valid GUID value: " + mappingElement.OuterXml);

      
this.ItemID = itemID;
    }

    [NotNull]
    protected override IEnumerable<JsonItem> Initialize([NotNull] string json)
    {
      Assert.ArgumentNotNull(json, "json");

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
      Assert.ArgumentNotNull(itemId, "itemId");

      if (itemId == this.ItemID)
      {
        return this.ItemChildren.Select(x => x.ID);
      }

      var item = this.GetItem(itemId);
      if (item != null)
      {
        return item.Children.Select(x => x.ID);
      }

      return null;
    }

    protected override bool IgnoreItem(JsonItem item)
    {
      return false;
    }


    public override bool CreateItem(ID itemID, string itemName, ID templateID, ID parentID)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      Assert.ArgumentNotNull(itemName, "itemName");
      Assert.ArgumentNotNull(templateID, "templateID");
      Assert.ArgumentNotNull(parentID, "parentID");

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
      Assert.ArgumentNotNull(sourceItemID, "sourceItemID");
      Assert.ArgumentNotNull(destinationItemID, "destinationItemID");
      Assert.ArgumentNotNull(copyID, "copyID");
      Assert.ArgumentNotNull(copyName, "copyName");

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

      if (!this.CreateItem(copyID, copyName, sourceItem.TemplateID, destinationItemID))
      {
        return false;
      }

      return this.DoCopyItem(destinationItemID, copyID, copyName, sourceItem);
    }

    public override bool MoveItem(ID itemID, ID targetID)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      Assert.ArgumentNotNull(targetID, "targetID");

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
          Assert.IsNotNull(target, "Moving item outside of ItemChildrenMapping ({0}, {1}) is not supported", this.ItemID, this.FileMappingPath);

          this.ItemChildren.Remove(item);
          target.Children.Add(item);
        }
        else if (targetID == this.ItemID)
        {
          var parent = this.GetItem(parentID);
          Assert.IsNotNull(parent, "Cannot find {0} item", parentID);

          parent.Children.Remove(item);
          this.ItemChildren.Add(item);
        }
        else
        {
          var parent = this.GetItem(parentID);
          Assert.IsNotNull(parent, "Cannot find {0} item", parentID);

          var target = this.GetItem(targetID);
          Assert.IsNotNull(targetID, "Moving item outside of ItemChildrenMapping ({0}, {1}) is not supported", this.ItemID, this.FileMappingPath);

          parent.Children.Remove(item);
          target.Children.Add(item);
        }

        this.Commit();
      }

      return true;
    }

    protected override void DoDeleteItem(JsonItem item)
    {
      Assert.ArgumentNotNull(item, "item");

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