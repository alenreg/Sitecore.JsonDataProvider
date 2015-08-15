namespace Sitecore.Support.Data.DataProviders
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Xml;

  using Sitecore;
  using Sitecore.Collections;
  using Sitecore.Data;
  using Sitecore.Data.DataProviders;
  using Sitecore.Data.SqlServer;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;

  [UsedImplicitly]
  public class CompositeDataProvider : SqlServerDataProvider
  {
    [NotNull]
    private readonly List<CompositeFile> files = new List<CompositeFile>();

    [UsedImplicitly]
    public CompositeDataProvider([NotNull] string connectionString, [NotNull] string databaseName)
      : base(connectionString)
    {
      Assert.ArgumentNotNull(connectionString, "connectionString");
      Assert.ArgumentNotNull(databaseName, "databaseName");
    }

    [UsedImplicitly]
    public void AddFile([NotNull] XmlNode node)
    {
      Assert.ArgumentNotNull(node, "node");

      var element = (XmlElement)node;
      var typeString = element.GetAttribute("type");
      Assert.IsNotNull(typeString, "typeString");

      var type = Type.GetType(typeString);
      Assert.IsNotNull(type, "type: " + typeString);

      var parameters = new object[] { node };
      var obj = (CompositeFile)Activator.CreateInstance(type, parameters);
      this.Files.Add(obj);
    }

    [NotNull]
    public override ItemDefinition GetItemDefinition([CanBeNull] ID itemId, [CanBeNull] CallContext context)
    {
      if (itemId as object == null || context == null)
      {
        return base.GetItemDefinition(itemId, context);
      }

      var item = this.FindItem(itemId);
      if (item == null)
      {
        return base.GetItemDefinition(itemId, context);
      }

      return new ItemDefinition(itemId, item.Name, item.TemplateId, ID.Null);
    }

    [CanBeNull]
    private CompositeItem FindItem([NotNull] ID itemId)
    {
      Assert.ArgumentNotNull(itemId, "itemId");

      foreach (var file in this.files)
      {
        Assert.IsNotNull(file, "file");
        var item = file.FindItem(itemId);
        if (item != null)
        {
          return item;
        }
      }

      return null;
    }

    [NotNull]
    public override VersionUriList GetItemVersions([CanBeNull] ItemDefinition itemDefinition, [CanBeNull] CallContext context)
    {
      if (itemDefinition == null || context == null)
      {
        return base.GetItemVersions(itemDefinition, context);
      }

      var item = FindItem(itemDefinition.ID);
      if (item == null)
      {
        return base.GetItemVersions(itemDefinition, context);
      }

      var list = new VersionUriList();
      var versionUris = item.Languages.SelectMany(y => y.Versions.Select(x => new VersionUri(Language.Parse(y.Language), new Sitecore.Data.Version(x.Number))));
      foreach (var versionUri in versionUris)
      {
        list.Add(versionUri);
      }

      return list;
    }

    [NotNull]
    public override IDList GetChildIDs([CanBeNull] ItemDefinition itemDefinition, [CanBeNull] CallContext context)
    {
      if (itemDefinition == null || context == null)
      {
        return base.GetChildIDs(itemDefinition, context);
      }

      var childIDs = base.GetChildIDs(itemDefinition, context) ?? new IDList();

      // merge sql and json items

      foreach (var file in this.files)
      {
        if (itemDefinition.ID == file.ParentID)
        {
          var rootItems = file.RootItems;
          Assert.IsNotNull(rootItems, "rootItems");

          foreach (var child in rootItems)
          {
            Assert.IsNotNull(child, "child");

            var id = child.ID;
            Assert.IsNotNull(id, "id");

            childIDs.Add(id);
          }
        }
        else
        {
          var item = file.FindItem(itemDefinition.ID);
          if (item == null)
          {
            return childIDs;
          }

          var children = item.Children;
          Assert.IsNotNull(children, "children");

          foreach (var child in children)
          {
            Assert.IsNotNull(child, "child");

            childIDs.Add(child.ID);
          }
        }
      }

      return childIDs;
    }

    [NotNull]
    public override ID GetParentID([CanBeNull] ItemDefinition itemDefinition, [CanBeNull] CallContext context)
    {
      if (itemDefinition == null || context == null)
      {
        return base.GetParentID(itemDefinition, context);
      }

      var parentId = this.FindParentItemId(itemDefinition.ID);
      if (parentId as object == null)
      {
        return base.GetParentID(itemDefinition, context);
      }

      return parentId;
    }

    [CanBeNull]
    public ID FindParentItemId([NotNull] ID itemId)
    {
      Assert.ArgumentNotNull(itemId, "itemId");

      // first scan level 1
      foreach (var file in this.files)
      {
        var rootItems = file.RootItems;
        var item = rootItems.SingleOrDefault(x => x.ID == itemId);
        if (item != null)
        {
          return file.ParentID;
        }
      }

      var item2 = this.FindItem(itemId);
      var parentId = item2 != null ? item2.ParentID : null;

      return parentId;
    }

    public override bool CreateItem([CanBeNull] ID itemID, [CanBeNull] string itemName, [CanBeNull] ID templateID, [CanBeNull] ItemDefinition parent, [CanBeNull] CallContext context)
    {
      if (itemID as object == null || string.IsNullOrEmpty(itemName) || templateID as object == null || parent == null || context == null)
      {
        return base.CreateItem(itemID, itemName, templateID, parent, context);
      }

      foreach (var file in this.files)
      {
        if (parent.ID == file.ParentID)
        {
          file.CreateRootItem(itemID, itemName, templateID, context);

          return true;
        }
      }

      var parentItem = this.FindItem(parent.ID);
      if (parentItem == null)
      {
        return base.CreateItem(itemID, itemName, templateID, parent, context);
      }
      
      parentItem.AddChild(itemID, itemName, context);

      return true;
    }

    public override int AddVersion([CanBeNull] ItemDefinition itemDefinition, [CanBeNull] VersionUri baseVersion, [CanBeNull] CallContext context)
    {
      if (itemDefinition == null || baseVersion == null || context == null)
      {
        return base.AddVersion(itemDefinition, baseVersion, context);
      }

      var item = this.FindItem(itemDefinition.ID);
      if (item == null)
      {
        return base.AddVersion(itemDefinition, baseVersion, context);
      }

      var version = item.AddVersion(baseVersion.Language, baseVersion.Version.Number);
      if (version == null)
      {
        return -1;
      }

      item.File.Commit();

      return version.Number;
    }

    public override bool DeleteItem([CanBeNull] ItemDefinition itemDefinition, [CanBeNull] CallContext context)
    {
      if (itemDefinition == null || context == null)
      {
        return base.DeleteItem(itemDefinition, context);
      }

      var item = this.FindItem(itemDefinition.ID);
      if (item == null)
      {
        return base.DeleteItem(itemDefinition, context);
      }

      var parent = item.Parent;
      if (parent == null)
      {
        this.DatabaseObject.RootItems.Remove(item);
        this.Commit();
        return true;
      }

      var children = parent.Children;
      Assert.IsNotNull(children, "children");

      children.Remove(item);

      this.Commit();
      return true;
    }
  }
}