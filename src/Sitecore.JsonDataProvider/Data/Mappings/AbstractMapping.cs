﻿namespace Sitecore.Data.Mappings
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Xml;

  using Sitecore.Collections;
  using Sitecore.Data.Collections;
  using Sitecore.Data.DataProviders;
  using Sitecore.Data.Helpers;
  using Sitecore.Data.Items;
  using Sitecore.Data.Templates;
  using Sitecore.Diagnostics;
  using Sitecore.Extensions;
  using Sitecore.Globalization;

  public abstract class AbstractMapping : IMapping
  {
    protected readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    [NotNull]
    protected readonly List<JsonItem> ItemChildren = new List<JsonItem>();

    [NotNull]
    protected internal readonly NullDictionary<ID, JsonItem> ItemsCache = new NullDictionary<ID, JsonItem>();

    public string DatabaseName { get; }

    public virtual bool ReadOnly { get; }

    public int ItemsCount
    {
      get
      {
        Lock.EnterReadLock();
        try
        {
          return ItemsCache.Count;
        }
        finally
        {
          Lock.ExitReadLock();
        }
      }
    }

    public virtual string DisplayName => $"{this.Name} mapping";

    public string Name { get; }

    protected AbstractMapping([NotNull] XmlElement mappingElement, [NotNull] string databaseName)
    {
      Assert.ArgumentNotNull(mappingElement, nameof(mappingElement));
      Assert.ArgumentNotNull(databaseName, nameof(databaseName));

      var readOnly = mappingElement.GetAttribute("readOnly") == "true";
      var name = mappingElement.GetAttribute("name");

      this.Name = name;
      this.ReadOnly = readOnly;
      this.DatabaseName = databaseName;
    }

    public abstract void Initialize();

    public abstract IEnumerable<ID> GetChildIDs(ID itemId);

    public IEnumerable<ID> GetIDs()
    {
      Lock.EnterReadLock();
      try
      {
        return this.ItemsCache.Keys;
      }
      finally
      {
        Lock.ExitReadLock();
      }
    }

    public IEnumerable<ID> ResolveNames(string itemName)
    {
      Assert.ArgumentNotNull(itemName, nameof(itemName));

      //TODO: optimize this
      Lock.EnterReadLock();
      try
      {
        return this.ItemsCache.Values.Where(x => x.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase)).Select(x => x.ID);
      }
      finally
      {
        Lock.ExitReadLock();
      }
    }

    public IEnumerable<ID> ResolvePath(string path, CallContext context)
    {
      Assert.ArgumentNotNull(path, nameof(path));
      Assert.ArgumentNotNull(context, nameof(context));

      var words = path.Split('/');
      if (words.Length == 0)
      {
        yield break;
      }

      var dataManager = context.DataManager;
      Assert.IsNotNull(dataManager, nameof(dataManager));

      var database = dataManager.Database;
      Assert.IsNotNull(database, nameof(database));

      Lock.EnterReadLock();
      try
      {
        foreach (var item in this.ItemChildren)
        {
          Assert.IsNotNull(item, nameof(item));

          var parentID = item.ParentID;
          if (parentID == ItemIDs.RootID)
          {
            if (item.Name.Equals(words.First(), StringComparison.OrdinalIgnoreCase))
            {
              var ids = ResolvePath(item.Children, words, 1);
              if (ids == null)
              {
                continue;
              }

              foreach (var id in ids)
              {
                yield return id;
              }
            }
          }
          else
          {
            var parentItem = database.GetItem(parentID);

            var parentPath = parentItem?.Paths?.FullPath;
            if (parentPath == null)
            {
              continue;
            }

            if (path.Equals(parentPath, StringComparison.OrdinalIgnoreCase))
            {
              yield return parentID;
            }
            else if (path.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase))
            {
              var ids = ResolvePath(item.Children, path.Substring(parentPath.Length + 1).Split('/'), 0);
              if (ids == null)
              {
                continue;
              }

              foreach (var id in ids)
              {
                yield return id;
              }
            }
          }
        }

      }
      finally
      {
        Lock.ExitReadLock();
      }
    }

    private IEnumerable<ID> ResolvePath([NotNull] JsonChildren children, [NotNull] string[] words, int index)
    {
      Assert.ArgumentNotNull(children, nameof(children));
      Assert.ArgumentNotNull(words, nameof(words));

      if (words.Length == index + 1)
      {
        var name = words[index];

        foreach (var id in children.Where(x => x.Name == name).Select(x => x.ID))
        {
          yield return id;
        }
      }

      if (words.Length <= index)
      {
        var name = words[index];
        foreach (var child in children.Where(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
          foreach (var id in ResolvePath(child.Children, words, index + 1))
          {
            yield return id;
          }
        }
      }
    }

    public ItemDefinition GetItemDefinition(ID itemID)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return null;
      }

      return new ItemDefinition(item.ID, item.Name, item.TemplateID, ID.Null);
    }

    public ID GetParentID(ID itemID)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return null;
      }

      return item.ParentID;
    }

    public VersionUriList GetItemVersions(ID itemID)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return null;
      }

      var versionUriList = new VersionUriList();

      Lock.EnterReadLock();
      try
      {
        foreach (var lang in item.Fields.Versioned)
        {
          var language = Language.Parse(lang.Key);
          var versions = lang.Value;

          foreach (var versionNumber in versions.Keys)
          {
            var version = new Sitecore.Data.Version(versionNumber);
            var versionUri = new VersionUri(language, version);

            versionUriList.Add(versionUri);
          }
        }
      }
      finally
      {
        Lock.ExitReadLock();
      }

      return versionUriList;
    }

    public FieldList GetItemFields(ID itemID, VersionUri versionUri)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(versionUri, nameof(versionUri));

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return null;
      }

      var fieldList = new FieldList();

      // add shared fields
      Lock.EnterReadLock();
      try
      {
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
      }
      finally
      {
        Lock.ExitReadLock();
      }

      return fieldList;
    }

    public IEnumerable<ID> GetTemplateItemIDs()
    {
      Lock.EnterReadLock();
      try
      {
        return this.ItemsCache.Values.Where(x => x.TemplateID == TemplateIDs.Template).Select(x => x.ID);
      }
      finally
      {
        Lock.ExitReadLock();
      }
    }

    public IEnumerable<Tuple<string, ID>> GetLanguages()
    {
      Lock.EnterReadLock();
      try
      {
        return this.ItemsCache.Values.Where(x => x.ParentID == ItemIDs.LanguageRoot && x.TemplateID == TemplateIDs.Language).Select(x => new Tuple<string, ID>(x.Name, x.ID));
      }
      finally
      {
        Lock.ExitReadLock();
      }
    }

    public abstract bool CreateItem(ID itemID, string itemName, ID templateID, ID parentID);

    public abstract bool CopyItem(ID sourceItemID, ID destinationItemID, ID copyID, string copyName, CallContext context);

    public int AddVersion(ID itemID, VersionUri versionUri)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(versionUri, nameof(versionUri));

      JsonItem item;

      item = this.GetItem(itemID);
      if (item == null)
      {
        return -1;
      }

      if (this.ReadOnly)
      {
        throw new InvalidOperationException($"The file mapping the {itemID} item is in read-only mode");
      }

      var newNumber = -1;
      var number = versionUri.Version.Number;
      var language = versionUri.Language;

      Lock.EnterWriteLock();
      var locked = true;
      try
      {
        var versions = item.Fields.Versioned[language];

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
          // unlock to commit
          Lock.ExitWriteLock();
          locked = false;

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

        var newVersion = new JsonFieldsCollection
        {
          [FieldIDs.Created] = DateUtil.IsoNowWithTicks
        };

        versions.Add(newNumber, newVersion);
      }
      finally
      {
        if (locked)
        {
          Lock.ExitWriteLock();
        }
      }

      this.Commit();

      return newNumber;
    }

    public bool SaveItem(ID itemID, ItemChanges changes)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(changes, nameof(changes));

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return false;
      }

      if (this.ReadOnly)
      {
        throw new InvalidOperationException($"The file mapping the {itemID} item belongs to is in read-only mode");
      }

      Lock.EnterWriteLock();
      try
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
            if (fieldID == Null.Object)
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
              versioned?.Remove(fieldID);
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
                versioned = new JsonFieldsCollection
                {
                  [FieldIDs.Created] = DateUtil.IsoNowWithTicks
                };

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
      }
      finally
      {
        Lock.ExitWriteLock();
      }

      this.Commit();

      return true;
    }

    public void ChangeFieldSharing(ID fieldID, TemplateFieldSharing sharing)
    {
      Assert.ArgumentNotNull(fieldID, nameof(fieldID));

      if (this.ReadOnly)
      {
        return;
      }

      Lock.EnterWriteLock();
      try
      {
        foreach (var item in this.ItemsCache.Values)
        {
          if (item == null)
          {
            continue;
          }

          switch (sharing)
          {
            case TemplateFieldSharing.None:
              this.ChangeFieldSharingToVersioned(item, fieldID);
              break;

            case TemplateFieldSharing.Unversioned:
              this.ChangeFieldSharingToUnversioned(item, fieldID);
              break;

            case TemplateFieldSharing.Shared:
              this.ChangeFieldSharingToShared(item, fieldID);
              break;
          }
        }
      }
      finally
      {
        Lock.ExitWriteLock();
      }

      this.Commit();
    }

    public abstract bool MoveItem(ID itemID, ID targetID);

    public bool RemoveVersion(ID itemID, VersionUri versionUri)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(versionUri, nameof(versionUri));

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return false;
      }

      if (this.ReadOnly)
      {
        throw new InvalidOperationException($"The file mapping the {itemID} item belongs to is in read-only mode");
      }

      var language = versionUri.Language;
      Assert.IsNotNull(language, "language");

      var version = versionUri.Version;
      Assert.IsNotNull(version, "version");

      Lock.EnterWriteLock();
      try
      {
        var versions = item.Fields.Versioned[language];

        if (!versions.Remove(version.Number))
        {
          return false;
        }
      }
      finally
      {
        Lock.ExitWriteLock();
      }

      this.Commit();

      return true;
    }

    public bool RemoveVersions(ID itemID, Language language)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(language, nameof(language));

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return false;
      }

      if (this.ReadOnly)
      {
        throw new InvalidOperationException($"The file mapping the {itemID} item belongs to is in read-only mode");
      }

      Lock.EnterWriteLock();
      try
      {
        if (language == Language.Invariant)
        {
          item.Fields.Versioned.Clear();
        }
        else
        {
          item.Fields.Versioned[language].Clear();
        }
      }
      finally
      {
        Lock.ExitWriteLock();
      }

      this.Commit();

      return true;
    }

    public bool DeleteItem(ID itemID)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));

      var item = this.GetItem(itemID);
      if (item == null)
      {
        return false;
      }

      if (this.ReadOnly)
      {
        throw new InvalidOperationException($"The file mapping the {itemID} item belongs to is in read-only mode");
      }

      Lock.EnterWriteLock();
      try
      {
        this.DoDeleteItem(item);

        this.DeleteItemTreeFromItemsCache(item);
      }
      finally
      {
        Lock.ExitWriteLock();
      }

      this.Commit();

      return true;
    }

    public abstract void Commit();

    public abstract bool AcceptsNewChildrenOf(ID itemID);

    protected abstract bool IgnoreItem([NotNull] JsonItem item);

    protected JsonItem DoCopy(ID sourceItemID, ID destinationItemID, ID copyID, string copyName, CallContext context)
    {
      var source = context.DataManager.Database.GetItem(sourceItemID);
      var item = new JsonItem(copyID, destinationItemID)
      {
        Name = copyName,
        TemplateID = source.TemplateID
      };

      var sharedIds = source.Template.Fields.Where(x => x.IsShared).Select(x => x.ID).ToArray();
      foreach (var fieldId in sharedIds)
      {
        if (JsonDataProvider.IgnoreFields.Keys.Contains(fieldId))
        {
          continue;
        }

        var field = source.Fields[fieldId];
        var value = field.GetValue(false, false, false, false);
        if (value == null)
        {
          continue;
        }

        item.Fields.Shared[fieldId] = value;
      }

      var unversionedIds = source.Template.Fields.Where(x => x.IsUnversioned).Select(x => x.ID).ToArray();
      foreach (var language in source.Languages)
      {
        var version = source.Versions.GetLatestVersion(language);
        var json = item.Fields.Unversioned[language];
        foreach (var fieldId in unversionedIds)
        {
          if (JsonDataProvider.IgnoreFields.Keys.Contains(fieldId))
          {
            continue;
          }

          var field = version.Fields[fieldId];
          var value = field.GetValue(false, false, false, false);
          if (value == null)
          {
            continue;
          }

          json[fieldId] = value;
        }
      }

      var versionedIds = source.Template.Fields.Where(x => !x.IsShared && !x.IsUnversioned).Select(x => x.ID).ToArray();
      foreach (var language in source.Languages)
      {
        var sourceVersion = source.Versions.GetLatestVersion(language);
        var jsonVersions = item.Fields.Versioned[language];
        foreach (var version in sourceVersion.Versions.GetVersions())
        {
          var json = new JsonFieldsCollection();
          foreach (var fieldId in versionedIds)
          {
            if (JsonDataProvider.IgnoreFields.Keys.Contains(fieldId))
            {
              continue;
            }

            var field = version.Fields[fieldId];
            var value = field.GetValue(false, false, false, false);
            if (value == null)
            {
              continue;
            }

            json[fieldId] = value;
          }

          jsonVersions.Add(version.Version.Number, json);
        }
      }

      return item;
    }

    protected abstract void DoDeleteItem([NotNull] JsonItem item);

    protected void InitializeItemTree([NotNull] JsonItem item)
    {
      Assert.ArgumentNotNull(item, nameof(item));

      // no lock needed
      this.ItemsCache[item.ID] = item;

      JsonDataProvider.InitializeDefaultValues(item.Fields);

      foreach (var child in item.Children)
      {
        if (child == null)
        {
          continue;
        }

        child.ParentID = item.ID;
        this.InitializeItemTree(child);
      }
    }

    [CanBeNull]
    protected JsonItem GetItem([NotNull] ID itemID)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));

      return GetItemRaw(itemID).OnlyIf(x => !this.IgnoreItem(x));
    }

    [CanBeNull]
    protected JsonItem GetItemRaw([NotNull] ID itemID)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));

      Lock.EnterReadLock();
      try
      {
        return this.ItemsCache[itemID];
      }
      finally
      {
        Lock.ExitReadLock();
      }
    }

    private void ChangeFieldSharingToShared([NotNull] JsonItem item, [NotNull] ID fieldID)
    {
      Assert.ArgumentNotNull(item, nameof(item));
      Assert.ArgumentNotNull(fieldID, nameof(fieldID));

      var fields = item.Fields;
      var fieldValue = Null.String;
      var allLanguages = fields.GetAllLanguages();
      foreach (var language in allLanguages)
      {
        if (fields.Unversioned.ContainsKey(language))
        {
          fieldValue = fields.Unversioned[language][fieldID];
        }

        if (fieldValue == null)
        {
          fieldValue = fields.Versioned.GetFieldValue(language, fieldID);
        }

        // if value was found then 
        if (fieldValue != null)
        {
          // set shared value
          fields.Shared[fieldID] = fieldValue;
        }
      }

      fields.Unversioned.RemoveField(fieldID);
      fields.Versioned.RemoveField(fieldID);
    }

    private void ChangeFieldSharingToUnversioned([NotNull] JsonItem item, [NotNull] ID fieldID)
    {
      Assert.ArgumentNotNull(item, nameof(item));
      Assert.ArgumentNotNull(fieldID, nameof(fieldID));

      // find value among shared
      var fields = item.Fields;
      var fieldValue = fields.Shared[fieldID];

      // if found among shared
      if (fieldValue != null)
      {
        // set same "shared" value in all languages
        foreach (var langGroup in fields.Unversioned)
        {
          var targetFields = langGroup.Value;
          if (targetFields != null)
          {
            targetFields[fieldID] = fieldValue;
          }
        }
      }
      else
      {
        // for all languages in versioned fields
        foreach (var langGroup in fields.Versioned)
        {
          var language = langGroup.Key;
          fieldValue = langGroup.Value.GetFieldValue(fieldID);

          // if value was found then 
          if (fieldValue != null)
          {
            // set unversioned value
            fields.Unversioned[language][fieldID] = fieldValue;
          }
        }
      }

      // remove value from shared fields
      fields.Shared.Remove(fieldID);
      fields.Versioned.RemoveField(fieldID);
    }

    private void ChangeFieldSharingToVersioned([NotNull] JsonItem item, [NotNull] ID fieldID)
    {
      Assert.ArgumentNotNull(item, nameof(item));
      Assert.ArgumentNotNull(fieldID, nameof(fieldID));

      // find value among shared
      var fields = item.Fields;
      var fieldValue = fields.Shared[fieldID];

      // if found among shared
      if (fieldValue != null)
      {
        // set same "shared" value in all versions of all languages
        foreach (var languageVersions in fields.Versioned.Values)
        {
          if (languageVersions == null)
          {
            continue;
          }

          foreach (var versionFields in languageVersions.Values)
          {
            if (versionFields != null)
            {
              versionFields[fieldID] = fieldValue;
            }
          }
        }
      }
      else // find value among unversioned
      {
        var allLanguages = fields.GetAllLanguages();
        foreach (var language in allLanguages)
        {
          if (fields.Unversioned.ContainsKey(language))
          {
            fieldValue = fields.Unversioned[language][fieldID];
          }

          // if value was found then 
          if (fieldValue != null)
          {
            // set unversioned value in all versions
            var versions = fields.Versioned[language];
            if (versions.Count == 0)
            {
              var fieldCollection = new JsonFieldsCollection
              {
                [FieldIDs.Created] = DateUtil.IsoNowWithTicks
              };

              versions.Add(1, fieldCollection);
            }

            foreach (var versionFields in versions.Values)
            {
              versionFields[fieldID] = fieldValue;
            }
          }
        }
      }

      // remove invalid values
      fields.Shared.Remove(fieldID);
      fields.Unversioned.RemoveField(fieldID);
    }

    private void DeleteItemTreeFromItemsCache([NotNull] JsonItem item)
    {
      Assert.ArgumentNotNull(item, nameof(item));

      this.ItemsCache.Remove(item.ID);
      foreach (var child in item.Children)
      {
        Assert.IsNotNull(child, "child");

        this.DeleteItemTreeFromItemsCache(child);
      }
    }
  }
}