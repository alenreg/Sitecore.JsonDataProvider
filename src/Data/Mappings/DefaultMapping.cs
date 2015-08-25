namespace Sitecore.Data.Mappings
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Xml;

  using Newtonsoft.Json;

  using Sitecore.Collections;
  using Sitecore.Data;
  using Sitecore.Data.Collections;
  using Sitecore.Data.Helpers;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;

  public class DefaultMapping : IMapping
  {
    [NotNull]
    public readonly string FileMappingPath;

    [NotNull]
    protected readonly List<JsonItem> ItemChildren = new List<JsonItem>();

    [NotNull]
    protected readonly List<JsonItem> ItemsCache = new List<JsonItem>();

    [NotNull]
    protected readonly object SyncRoot = new object();

    [UsedImplicitly]
    public DefaultMapping([NotNull] XmlElement mappingElement)
    {
      Assert.ArgumentNotNull(mappingElement, "mappingElement");

      var fileName = mappingElement.GetAttribute("file");
      Assert.IsNotNullOrEmpty(fileName, "The \"file\" attribute is not specified or has empty string value: " + mappingElement.OuterXml);

      var filePath = MainUtil.MapPath(fileName);
      Assert.IsNotNullOrEmpty(filePath, "filePath");

      this.FileMappingPath = filePath;

      Log.Info("Default mapping is constructed with " + filePath, this);

      if (!File.Exists(filePath))
      {
        return;
      }

      try
      {
        Log.Info("Deserializing items from: " + filePath, this);

        var json = File.ReadAllText(filePath);
        var dictionary = JsonHelper.Deserialize<IDictionary<string, List<JsonItem>>>(json);
        if (dictionary == null)
        {
          return;
        }

        var children = dictionary
          .Where(x => x.Key as object != null && x.Value != null)
          .Select(x => new JsonItem(ID.Parse(x.Key), ID.Null, x.Value))
          .ToList();

        this.ItemChildren = children;
        foreach (var item in children)
        {
          item.ParentID = ID.Null;
          this.Initialize(item);
        }
      }
      catch (Exception ex)
      {
        Log.Error("Cannot deserialize json file: " + filePath, ex, this);

        throw;
      }
    }


    public IEnumerable<ID> GetChildIDs(ID itemId)
    {
      Assert.ArgumentNotNull(itemId, "itemId");

      var item = this.GetItem(itemId);

      // no need to check: item.ParentID == ID.Null
      if (item != null)
      {
        return item.Children.Select(x => x.ID);
      }

      return null;
    }


    public ItemDefinition GetItemDefinition(ID itemID)
    {
      Assert.ArgumentNotNull(itemID, "itemID");

      var item = this.GetItem(itemID);
      if (item == null || item.ParentID == ID.Null)
      {
        return null;
      }

      return new ItemDefinition(item.ID, item.Name, item.TemplateID, ID.Null);
    }


    public ID GetParentID(ID itemID)
    {
      Assert.ArgumentNotNull(itemID, "itemID");

      var item = this.GetItem(itemID);
      if (item == null || item.ParentID == ID.Null)
      {
        return null;
      }

      return item.ParentID;
    }


    public VersionUriList GetItemVersiones(ID itemID)
    {
      Assert.ArgumentNotNull(itemID, "itemID");

      var item = this.GetItem(itemID);
      if (item == null || item.ParentID == ID.Null)
      {
        return null;
      }

      var versionUriList = new VersionUriList();
      var versions = item.Fields.Versioned.SelectMany(lang => lang.Value.Select(ver => new VersionUri(Language.Parse(lang.Key), new Sitecore.Data.Version(ver.Key))));
      foreach (var versionUri in versions)
      {
        versionUriList.Add(versionUri);
      }

      return versionUriList;
    }


    public FieldList GetItemFields(ID itemID, VersionUri versionUri)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      Assert.ArgumentNotNull(versionUri, "versionUri");

      var item = this.GetItem(itemID);
      if (item == null || item.ParentID == ID.Null)
      {
        return null;
      }

      var fieldList = new FieldList();

      // add shared fields
      foreach (var field in item.Fields.Shared)
      {
        fieldList.Add(field.Key, field.Value);
      }

      var language = versionUri.Language;
      Assert.IsNotNull(language, "language");

      if (language == Language.Invariant)
      {
        return fieldList;
      }

      // add unversioned fields
      foreach (var field in item.Fields.Unversioned[language])
      {
        fieldList.Add(field.Key, field.Value);
      }

      var number = versionUri.Version.Number;
      var version = item.Fields.Versioned[language][number];

      if (version == null)
      {
        return fieldList;
      }

      // add versioned fields
      foreach (var field in version)
      {
        fieldList.Add(field.Key, field.Value);
      }

      return fieldList;
    }


    public IEnumerable<ID> GetTemplateItemIDs()
    {
      return this.ItemsCache.Where(x => x.TemplateID == TemplateIDs.Template).Select(x => x.ID);
    }

    public bool CreateItem(ID itemID, string itemName, ID templateID, ID parentID)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      Assert.ArgumentNotNull(itemName, "itemName");
      Assert.ArgumentNotNull(templateID, "templateID");
      Assert.ArgumentNotNull(parentID, "parentID");

      var parent = this.ItemsCache.FirstOrDefault(x => x.ID == parentID);

      // no need to check: parent.ParentID == ID.Null
      if (parent == null)
      {
        // return false;
        parent = this.AddRootItem(parentID);
      }

      var item = new JsonItem(itemID, parentID)
        {
          Name = itemName,
          TemplateID = templateID
        };

      lock (this.SyncRoot)
      {
        this.ItemsCache.Add(item);

        parent.Children.Add(item);

        this.Commit();
      }

      return true;
    }

    public bool CopyItem(ID sourceItemID, ID destinationItemID, ID copyID, string copyName)
    {
      Assert.ArgumentNotNull(sourceItemID, "sourceItemID");
      Assert.ArgumentNotNull(destinationItemID, "destinationItemID");
      Assert.ArgumentNotNull(copyID, "copyID");
      Assert.ArgumentNotNull(copyName, "copyName");

      var sourceItem = this.GetItem(sourceItemID);
      if (sourceItem == null || sourceItem.ParentID == ID.Null)
      {
        return false;
      }

      var destinationItem = this.GetItem(destinationItemID);

      // no need to check: destinationItem.ParentID == ID.Null
      if (destinationItem == null)
      {
        return false;
      }

      if (!this.CreateItem(copyID, copyName, sourceItem.TemplateID, destinationItemID))
      {
        return false;
      }

      var copyItem = this.GetItem(copyID);
      var copyFields = copyItem.Fields;
      var copyShared = copyFields.Shared;
      var copyUnversioned = copyFields.Unversioned;
      var copyVersioned = copyFields.Versioned;
      var sourceFields = sourceItem.Fields;
      lock (this.SyncRoot)
      {
        // copy shared fields
        copyShared.Clear();
        foreach (var sourceField in sourceFields.Shared)
        {
          copyShared.Add(sourceField.Key, sourceField.Value);
        }

        // copy unversioned fields
        foreach (var languageGroup in sourceFields.Unversioned)
        {
          var language = languageGroup.Key;
          var fields = copyUnversioned[language];
          foreach (var sourceField in languageGroup.Value)
          {
            fields.Add(sourceField.Key, sourceField.Value);
          }
        }

        // copy versioned
        foreach (var languageGroup in sourceFields.Versioned)
        {
          var language = languageGroup.Key;
          var versions = copyVersioned[language];
          foreach (var versionGroup in languageGroup.Value)
          {
            var number = versionGroup.Key;
            var fields = new JsonFieldsCollection();
            versions.Add(number, fields);
            foreach (var sourceField in versionGroup.Value)
            {
              fields.Add(sourceField.Key, sourceField.Value);
            }
          }
        }

        this.Commit();
      }

      return true;
    }

    public int AddVersion(ID itemID, VersionUri versionUri)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      Assert.ArgumentNotNull(versionUri, "versionUri");

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return -1;
      }

      var newNumber = -1;
      var number = versionUri.Version.Number;
      var language = versionUri.Language;

      var versions = item.Fields.Versioned[language];

      lock (this.SyncRoot)
      {
        if (number > 0)
        {
          // command to try to copy existing version
          var version = versions[number];
          if (version != null)
          {
            newNumber = versions.Max(x => x.Key) + 1;

            var copiedVersion = new JsonFieldsCollection(version);
            copiedVersion.Remove(FieldIDs.WorkflowState);

            versions.Add(newNumber, copiedVersion);
          }
        }

        if (newNumber != -1)
        {
          this.Commit();

          return newNumber;
        }

        if (versions.Count == 0)
        {
          newNumber = 1;
        }
        else
        {
          newNumber = versions.Max(x => x.Key) + 1;
        }

        var newVersion = new JsonFieldsCollection();
        newVersion[FieldIDs.Created] = DateUtil.IsoNowWithTicks;

        versions.Add(newNumber, newVersion);

        this.Commit();

        return newNumber;
      }
    }

    public bool SaveItem(ID itemID, ItemChanges changes)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      Assert.ArgumentNotNull(changes, "changes");

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return false;
      }

      lock (this.SyncRoot)
      {
        if (changes.HasPropertiesChanged)
        {
          var name = changes.GetPropertyValue("name") as string;
          item.Name = name ?? item.Name;

          var templateID = changes.GetPropertyValue("templateid") as ID;
          item.TemplateID = templateID ?? item.TemplateID;
        }

        if (changes.HasFieldsChanged)
        {
          var saveAll = changes.Item.RuntimeSettings.SaveAll;
          if (saveAll)
          {
            item.Fields.Shared.Clear();
            item.Fields.Unversioned.Clear();
            item.Fields.Versioned.Clear();
          }

          foreach (var fieldChange in changes.FieldChanges.OfType<FieldChange>())
          {
            var language = fieldChange.Language;
            var number = fieldChange.Version.Number;
            var fieldID = fieldChange.FieldID;
            if (fieldID as object == null)
            {
              continue;
            }

            var definition = fieldChange.Definition;
            if (definition == null)
            {
              continue;
            }

            var value = fieldChange.Value;
            var shared = item.Fields.Shared;
            var unversioned = item.Fields.Unversioned[language];
            var versions = item.Fields.Versioned[language];
            var versioned = versions[number];

            if (fieldChange.RemoveField || value == null)
            {
              if (saveAll)
              {
                continue;
              }

              shared.Remove(fieldID);
              unversioned.Remove(fieldID);
              if (versioned != null)
              {
                versioned.Remove(fieldID);
              }
            }
            else if (definition.IsShared)
            {
              shared[fieldID] = value;
            }
            else if (definition.IsUnversioned)
            {
              unversioned[fieldID] = value;
            }
            else if (definition.IsVersioned)
            {
              if (versioned == null)
              {
                versioned = new JsonFieldsCollection();
                versions.Add(number, versioned);
              }

              versioned[fieldID] = value;
            }
            else
            {
              throw new NotSupportedException("This situation is not supported");
            }
          }
        }

        this.Commit();
      }

      return true;
    }

    public bool MoveItem(ID itemID, ID targetID)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      Assert.ArgumentNotNull(targetID, "targetID");

      var item = this.GetItem(itemID);
      if (item == null || item.ParentID == ID.Null)
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
      
      lock (this.SyncRoot)
      {
        parent.Children.Remove(item);
        target.Children.Add(item);

        this.Commit();
      }

      return true;
    }

    public bool RemoveVersion(ID itemID, VersionUri versionUri)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      Assert.ArgumentNotNull(versionUri, "versionUri");

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return false;
      }

      var language = versionUri.Language;
      Assert.IsNotNull(language, "language");

      var version = versionUri.Version;
      Assert.IsNotNull(version, "version");

      var versions = item.Fields.Versioned[language];

      lock (this.SyncRoot)
      {
        if (!versions.Remove(version.Number))
        {
          return false;
        }

        this.Commit();
      }

      return true;
    }

    public bool RemoveVersions(ID itemID, Language language)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      Assert.ArgumentNotNull(language, "language");

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return false;
      }

      lock (this.SyncRoot)
      {
        if (language == Language.Invariant)
        {
          item.Fields.Versioned.Clear();
        }
        else
        {
          item.Fields.Versioned[language].Clear();
        }

        this.Commit();
      }

      return true;
    }

    public bool DeleteItem(ID itemID)
    {
      Assert.ArgumentNotNull(itemID, "itemID");

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return false;
      }

      lock (this.SyncRoot)
      {
        var parentID = item.ParentID;
        var parent = this.GetItem(parentID);
        Assert.IsNotNull(parent, "parent");

        parent.Children.Remove(item);

        this.DeleteItem(item);

        this.Commit();
      }

      return true;
    }

    private void Initialize([NotNull] JsonItem item)
    {
      Assert.ArgumentNotNull(item, "item");

      this.ItemsCache.Add(item);

      item.Fields.Shared[Settings.ItemStyleFieldID] = Settings.ItemStyleValue;

      foreach (var child in item.Children)
      {
        if (child == null)
        {
          continue;
        }

        child.ParentID = item.ID;
        this.Initialize(child);
      }
    }

    private void Commit()
    {
      var filePath = this.FileMappingPath;
      var directory = Path.GetDirectoryName(filePath);
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      var json = JsonHelper.Serialize(this.ItemChildren.ToDictionary(x => x.ID.ToString(), x => x.Children), true);
      File.WriteAllText(filePath, json);
    }

    [CanBeNull]
    private JsonItem GetItem([NotNull] ID itemID)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      return this.ItemsCache.FirstOrDefault(x => x.ID == itemID);
    }

    [NotNull]
    private JsonItem AddRootItem([NotNull] ID itemID)
    {
      Assert.ArgumentNotNull(itemID, "itemID");

      var rootItem = new JsonItem(itemID, ID.Null)
        {
          Name = "$default-mapping"
        };
      
      this.ItemChildren.Add(rootItem);
      this.ItemsCache.Add(rootItem);

      return rootItem;
    }

    private void DeleteItem([NotNull] JsonItem item)
    {
      Assert.ArgumentNotNull(item, "item");

      this.ItemsCache.Remove(item);
      foreach (var child in item.Children)
      {
        Assert.IsNotNull(child, "child");

        this.DeleteItem(child);
      }
    }
  }
}