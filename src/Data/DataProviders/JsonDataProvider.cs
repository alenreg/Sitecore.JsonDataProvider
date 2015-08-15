namespace Sitecore.Support.Data.DataProviders
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Xml;

  using Sitecore.Collections;
  using Sitecore.Data;
  using Sitecore.Data.DataProviders;
  using Sitecore.Data.Items;
  using Sitecore.Data.SqlServer;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;

  [UsedImplicitly]
  public class JsonDataProvider : SqlServerDataProvider
  {
    [NotNull]
    public readonly List<JsonSegment> Segments = new List<JsonSegment>();

    public JsonDataProvider([NotNull] string connectionString, [NotNull] string databaseName)
      : base(connectionString)
    {
      Assert.ArgumentNotNull(connectionString, "connectionString");
      Assert.ArgumentNotNull(databaseName, "databaseName");

      Log.Info(string.Format("JsonDataProvider is being initialized for \"{0}\" database", databaseName), this);
    }

    [UsedImplicitly]
    public void AddSegment([NotNull] XmlNode componentNode)
    {
      Assert.ArgumentNotNull(componentNode, "componentNode");

      var segmentElement = (XmlElement)componentNode;
      var rootString = segmentElement.GetAttribute("root");
      Assert.IsNotNull(rootString, "root attribute is not specified or has empty string value: " + segmentElement.OuterXml);

      ID root;
      Assert.IsTrue(ID.TryParse(rootString, out root), "root attribute is not a valid GUID value: " + segmentElement.OuterXml);

      var file = segmentElement.GetAttribute("file");
      Assert.IsNotNullOrEmpty(file, "file attribute is not specified or has empty string value: " + segmentElement.OuterXml);

      var filePath = MainUtil.MapPath(file);
      var segment = new JsonSegment(root, filePath);

      this.Segments.Add(segment);
    }

    [NotNull]
    public override IDList GetChildIDs([NotNull] ItemDefinition itemDefinition, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
      Assert.ArgumentNotNull(context, "context");

      var itemId = itemDefinition.ID;
      Assert.IsNotNull(itemId, "itemId");

      var childIDs = new IDList();

      // several segments can point to same root so let's collect items from all of them
      foreach (var segment in this.Segments)
      {
        Assert.IsNotNull(segment, "segment");

        var segmentChildIDs = segment.GetChildIDs(itemId);
        if (segmentChildIDs == null)
        {
          continue;
        }

        foreach (var childID in segmentChildIDs)
        {
          Assert.IsNotNull(childID, "childID");

          childIDs.Add(childID);
        }
      }

      // check if SQL has items too
      var sqlChildIDs = base.GetChildIDs(itemDefinition, context);
      if (sqlChildIDs == null)
      {
        return childIDs;
      }

      // merge segments' items with ones from SQL
      foreach (var id in sqlChildIDs.Cast<ID>())
      {
        childIDs.Add(id);
      }

      return childIDs;
    }

    [CanBeNull]
    public override ItemDefinition GetItemDefinition([NotNull] ID itemID, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      Assert.ArgumentNotNull(context, "context");

      foreach (var segment in this.Segments)
      {
        Assert.IsNotNull(segment, "segment");

        var definition = segment.GetItemDefinition(itemID);
        if (definition != null)
        {
          return definition;
        }
      }

      return base.GetItemDefinition(itemID, context);
    }

    [CanBeNull]
    public override ID GetParentID([NotNull] ItemDefinition itemDefinition, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
      Assert.ArgumentNotNull(context, "context");

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var segment in this.Segments)
      {
        Assert.IsNotNull(segment, "segment");

        var parentID = segment.GetParentID(itemID);
        if (parentID as object != null)
        {
          return parentID;
        }
      }

      return base.GetParentID(itemDefinition, context);
    }

    [CanBeNull]
    public override VersionUriList GetItemVersions([NotNull] ItemDefinition itemDefinition, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
      Assert.ArgumentNotNull(context, "context");

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var segment in this.Segments)
      {
        Assert.IsNotNull(segment, "segment");

        var itemVersions = segment.GetItemVersiones(itemID);
        if (itemVersions != null)
        {
          return itemVersions;
        }
      }

      return base.GetItemVersions(itemDefinition, context);
    }

    [NotNull]
    public override FieldList GetItemFields([NotNull] ItemDefinition itemDefinition, [NotNull] VersionUri versionUri, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
      Assert.ArgumentNotNull(versionUri, "versionUri");
      Assert.ArgumentNotNull(context, "context");

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var segment in this.Segments)
      {
        Assert.IsNotNull(segment, "segment");

        var fieldList = segment.GetItemFields(itemID, versionUri);
        if (fieldList != null)
        {
          return fieldList;
        }
      }

      return base.GetItemFields(itemDefinition, versionUri, context);
    }

    [NotNull]
    public override IdCollection GetTemplateItemIds([NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(context, "context");

      var templateItemIDs = new IdCollection();
      foreach (var segment in this.Segments)
      {
        Assert.IsNotNull(segment, "segment");

        var segmentTemplateIDs = segment.GetTemplateItemIDs();
        if (segmentTemplateIDs != null)
        {
          foreach (var templateID in segmentTemplateIDs)
          {
            templateItemIDs.Add(templateID);
          }
        }
      }

      // merge json template IDs with sql template IDs
      var sqlTemplateItemIDs = base.GetTemplateItemIds(context);
      if (sqlTemplateItemIDs != null)
      {
        foreach (var templateID in sqlTemplateItemIDs)
        {
          templateItemIDs.Add(templateID);
        }
      }

      return templateItemIDs;
    }

    public override bool CreateItem([NotNull] ID itemID, [NotNull] string itemName, [NotNull] ID templateID, [NotNull] ItemDefinition parent, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      Assert.ArgumentNotNull(itemName, "itemName");
      Assert.ArgumentNotNull(templateID, "templateID");
      Assert.ArgumentNotNull(parent, "parent");
      Assert.ArgumentNotNull(context, "context");

      var parentId = parent.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var segment in this.Segments)
      {
        Assert.IsNotNull(segment, "segment");

        if (segment.CreateItem(itemID, itemName, templateID, parentId))
        {
          return true;
        }
      }

      return base.CreateItem(itemID, itemName, templateID, parent, context);
    }

    public override bool CopyItem([NotNull] ItemDefinition source, [NotNull] ItemDefinition destination, [NotNull] string copyName, [NotNull] ID copyID, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(source, "source");
      Assert.ArgumentNotNull(destination, "destination");
      Assert.ArgumentNotNull(copyName, "copyName");
      Assert.ArgumentNotNull(copyID, "copyID");
      Assert.ArgumentNotNull(context, "context");

      var sourceItemID = source.ID;
      Assert.IsNotNull(sourceItemID, "sourceItemID");

      var destinationItemID = destination.ID;
      Assert.IsNotNull(destinationItemID, "destinationItemID");

      foreach (var segment in this.Segments)
      {
        Assert.IsNotNull(segment, "segment");

        if (segment.CopyItem(sourceItemID, destinationItemID, copyID, copyName))
        {
          return true;
        }
      }

      return base.CopyItem(source, destination, copyName, copyID, context);
    }

    public override int AddVersion([NotNull] ItemDefinition itemDefinition, [NotNull] VersionUri baseVersion, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
      Assert.ArgumentNotNull(baseVersion, "baseVersion");
      Assert.ArgumentNotNull(context, "context");

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var segment in this.Segments)
      {
        Assert.IsNotNull(segment, "segment");

        var versionNumber = segment.AddVersion(itemID, baseVersion);
        if (versionNumber != -1)
        {
          return versionNumber;
        }
      }

      return base.AddVersion(itemDefinition, baseVersion, context);
    }

    public override bool SaveItem([NotNull] ItemDefinition itemDefinition, [NotNull] ItemChanges changes, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
      Assert.ArgumentNotNull(changes, "changes");
      Assert.ArgumentNotNull(context, "context");

      if (changes.HasPropertiesChanged || changes.HasFieldsChanged)
      {
        var itemID = itemDefinition.ID;
        Assert.IsNotNull(itemID, "itemID");

        foreach (var segment in this.Segments)
        {
          Assert.IsNotNull(segment, "segment");

          if (segment.SaveItem(itemID, changes))
          {
            return true;
          }
        }
      }

      return base.SaveItem(itemDefinition, changes, context);
    }

    public override bool RemoveVersion([NotNull] ItemDefinition itemDefinition, [NotNull] VersionUri versionUri, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
      Assert.ArgumentNotNull(versionUri, "versionUri");
      Assert.ArgumentNotNull(context, "context");

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var segment in this.Segments)
      {
        Assert.IsNotNull(segment, "segment");

        if (segment.RemoveVersion(itemID, versionUri))
        {
          return true;
        }
      }

      return base.RemoveVersion(itemDefinition, versionUri, context);
    }

    public override bool RemoveVersions([NotNull] ItemDefinition itemDefinition, [NotNull] Language language, bool removeSharedData, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
      Assert.ArgumentNotNull(language, "language");
      Assert.ArgumentNotNull(context, "context");

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var segment in this.Segments)
      {
        Assert.IsNotNull(segment, "segment");

        if (segment.RemoveVersions(itemID, language))
        {
          return true;
        }
      }

      return base.RemoveVersions(itemDefinition, language, removeSharedData, context);
    }

    public override bool DeleteItem([NotNull] ItemDefinition itemDefinition, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
      Assert.ArgumentNotNull(context, "context");

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var segment in this.Segments)
      {
        Assert.IsNotNull(segment, "segment");

        if (segment.DeleteItem(itemID))
        {
          return true;
        }
      }

      return base.DeleteItem(itemDefinition, context);
    }
  }
}