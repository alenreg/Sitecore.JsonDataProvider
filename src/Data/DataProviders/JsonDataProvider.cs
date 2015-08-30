namespace Sitecore.Data.DataProviders
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Xml;

  using Sitecore.Collections;
  using Sitecore.Configuration;
  using Sitecore.Data;
  using Sitecore.Data.Helpers;
  using Sitecore.Data.Items;
  using Sitecore.Data.Mappings;
  using Sitecore.Data.SqlServer;
  using Sitecore.Data.Templates;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Resources.Media;
  using Sitecore.StringExtensions;

  [UsedImplicitly]
  public class JsonDataProvider : SqlServerDataProvider
  {
    [NotNull]
    public static readonly Dictionary<string, Type> MappingTypes = new Dictionary<string, Type>();

    [NotNull]
    public static readonly IDictionary<ID, DefaultFieldValue> IgnoreFields = new Dictionary<ID, DefaultFieldValue>();

    [NotNull]
    public readonly IList<IMapping> FileMappings = new List<IMapping>();

    [NotNull]
    private static readonly List<JsonDataProvider> instances = new List<JsonDataProvider>();

    [NotNull]
    public static IReadOnlyCollection<JsonDataProvider> Instances
    {
      get
      {
        return instances;
      }
    }

    public static bool BetterMerging { get; private set; }

    public JsonDataProvider([NotNull] string connectionString, [NotNull] string databaseName, [NotNull] string betterMerging)
      : base(connectionString)
    {
      Assert.ArgumentNotNull(connectionString, nameof(connectionString));
      Assert.ArgumentNotNull(databaseName, nameof(databaseName));
      Assert.ArgumentNotNull(betterMerging, nameof(betterMerging));

      instances.Add(this);

      BetterMerging = bool.Parse(betterMerging);

      Log.Info(string.Format("JsonDataProvider is being initialized for \"{0}\" database", databaseName), this);
    }

    public JsonDataProvider([NotNull] string connectionString, [NotNull] string databaseName)
      : this(connectionString, databaseName, "true")
    {
      Assert.ArgumentNotNull(connectionString, nameof(connectionString));
      Assert.ArgumentNotNull(databaseName, nameof(databaseName));
    }

    [UsedImplicitly]
    public void AddFileMappingType([NotNull] XmlNode mappingTypeNode)
    {
      Assert.ArgumentNotNull(mappingTypeNode, nameof(mappingTypeNode));

      var mappingElement = (XmlElement)mappingTypeNode;
      var mappingName = mappingElement.Name;
      Assert.IsNotNullOrEmpty(mappingName, "mappingName");

      var typeString = mappingElement.GetAttribute("type");
      Assert.IsNotNullOrEmpty(typeString, "The \"type\" attribute is not specified: " + mappingElement.OuterXml);

      var type = Type.GetType(typeString);
      Assert.IsNotNull(type, "The type cannot be found: " + mappingElement.OuterXml);

      Log.Info("Mapping type is registered: " + mappingName, this);

      if (MappingTypes.ContainsKey(mappingName))
      {
        MappingTypes.Remove(mappingName);
      }

      MappingTypes.Add(mappingName, type);
    }

    [UsedImplicitly]
    public void AddIgnoreField([NotNull] XmlNode fieldNode)
    {
      Assert.ArgumentNotNull(fieldNode, nameof(fieldNode));

      var fieldElement = (XmlElement)fieldNode;

      var idString = StringUtil.GetString(fieldElement.GetAttribute("fieldID"), fieldElement.InnerText);
      Assert.IsNotNull(idString, "Neither fieldID attribute nor node inner text is not specified or has empty string value: " + fieldElement.OuterXml);

      ID fieldID;
      Assert.IsTrue(ID.TryParse(idString, out fieldID), "Neither fieldID attribute nor node inner text value is a valid GUID value: " + fieldElement.OuterXml);

      Log.Info("Ignore field is registered: " + idString, this);
      IgnoreFields[fieldID] = DefaultFieldValue.Parse(fieldElement);
    }

    [UsedImplicitly]
    public void AddFileMapping([NotNull] XmlNode mappingNode)
    {
      Assert.ArgumentNotNull(mappingNode, nameof(mappingNode));

      var mappingElement = (XmlElement)mappingNode;
      var mappingName = mappingNode.Name;
      Assert.IsNotNullOrEmpty(mappingName, "mappingName");

      Type mappingType;
      MappingTypes.TryGetValue(mappingName, out mappingType);
      Assert.IsNotNull(mappingType, "The {0} mapping type is not registered in <FileMappingTypes> element".FormatWith(mappingName));

      var mapping = (IMapping)Activator.CreateInstance(mappingType, new[] { mappingElement });
      Assert.IsNotNull(mapping, "mapping");

      Log.Info("Mapping is estabileshed: " + mappingName, this);
      this.FileMappings.Insert(0, mapping);
      mapping.Initialize();
    }

    [NotNull]
    public override IDList GetChildIDs([NotNull] ItemDefinition itemDefinition, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(context, nameof(context));

      var itemId = itemDefinition.ID;
      Assert.IsNotNull(itemId, "itemId");

      var childIDs = new IDList();

      // several fileMappings can point to same item so let's collect items from all of them
      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        var fileChildIDs = file.GetChildIDs(itemId);
        if (fileChildIDs == null)
        {
          continue;
        }

        foreach (var childID in fileChildIDs)
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

      // merge fileMappings' items with ones from SQL
      foreach (var id in sqlChildIDs.Cast<ID>())
      {
        childIDs.Add(id);
      }

      return childIDs;
    }

    [CanBeNull]
    public override ItemDefinition GetItemDefinition([NotNull] ID itemID, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(context, nameof(context));

      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        var definition = file.GetItemDefinition(itemID);
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
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(context, nameof(context));

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        var parentID = file.GetParentID(itemID);
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
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(context, nameof(context));

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        var itemVersions = file.GetItemVersiones(itemID);
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
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(versionUri, nameof(versionUri));
      Assert.ArgumentNotNull(context, nameof(context));

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        var fieldList = file.GetItemFields(itemID, versionUri);
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
      Assert.ArgumentNotNull(context, nameof(context));

      var templateItemIDs = new IdCollection();
      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        var fileTemplateIDs = file.GetTemplateItemIDs();
        if (fileTemplateIDs != null)
        {
          foreach (var templateID in fileTemplateIDs)
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

    [NotNull]
    public override LanguageCollection GetLanguages([NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(context, nameof(context));

      var languages = new LanguageCollection();
      foreach (var mapping in this.FileMappings)
      {
        var jsonLanguages = mapping.GetLanguages();
        foreach (var jsonLanguage in jsonLanguages)
        {
          var language = Language.Parse(jsonLanguage);
          if (!languages.Contains(language))
          {
            languages.Add(language);
          }
        }
      }

      var sqlLanguages = base.GetLanguages(context);
      if (sqlLanguages != null)
      {
        foreach (var language in sqlLanguages)
        {
          if (!languages.Contains(language))
          {
            languages.Add(language);
          }
        }
      }

      return languages;
    }

    public override Stream GetBlobStream(Guid blobId, CallContext context)
    {
      var filePath = GetBlobFilePath(blobId);
      if (File.Exists(filePath))
      {
        return File.OpenRead(filePath);
      }      

      return base.GetBlobStream(blobId, context);
    }

    public override bool SetBlobStream(Stream stream, Guid blobId, CallContext context)
    {
      var filePath = GetBlobFilePath(blobId);
      var folderPath = Path.GetDirectoryName(filePath);
      if (!Directory.Exists(folderPath))
      {
        Directory.CreateDirectory(folderPath);
      }

      using (var outputStream = File.OpenWrite(filePath))
      {
        stream.CopyTo(outputStream);
      }

      return true;
    }

    public override bool CreateItem([NotNull] ID itemID, [NotNull] string itemName, [NotNull] ID templateID, [NotNull] ItemDefinition parent, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(itemName, nameof(itemName));
      Assert.ArgumentNotNull(templateID, nameof(templateID));
      Assert.ArgumentNotNull(parent, nameof(parent));
      Assert.ArgumentNotNull(context, nameof(context));

      var parentId = parent.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        if (file.CreateItem(itemID, itemName, templateID, parentId))
        {
          return true;
        }
      }

      return base.CreateItem(itemID, itemName, templateID, parent, context);
    }

    public override bool CopyItem([NotNull] ItemDefinition source, [NotNull] ItemDefinition destination, [NotNull] string copyName, [NotNull] ID copyID, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(source, nameof(source));
      Assert.ArgumentNotNull(destination, nameof(destination));
      Assert.ArgumentNotNull(copyName, nameof(copyName));
      Assert.ArgumentNotNull(copyID, nameof(copyID));
      Assert.ArgumentNotNull(context, nameof(context));

      var sourceItemID = source.ID;
      Assert.IsNotNull(sourceItemID, "sourceItemID");

      var destinationItemID = destination.ID;
      Assert.IsNotNull(destinationItemID, "destinationItemID");

      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        if (file.CopyItem(sourceItemID, destinationItemID, copyID, copyName))
        {
          return true;
        }
      }

      return base.CopyItem(source, destination, copyName, copyID, context);
    }

    public override int AddVersion([NotNull] ItemDefinition itemDefinition, [NotNull] VersionUri baseVersion, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(baseVersion, nameof(baseVersion));
      Assert.ArgumentNotNull(context, nameof(context));

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        var versionNumber = file.AddVersion(itemID, baseVersion);
        if (versionNumber != -1)
        {
          return versionNumber;
        }
      }

      return base.AddVersion(itemDefinition, baseVersion, context);
    }

    public override bool SaveItem([NotNull] ItemDefinition itemDefinition, [NotNull] ItemChanges changes, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(changes, nameof(changes));
      Assert.ArgumentNotNull(context, nameof(context));

      if (changes.HasPropertiesChanged || changes.HasFieldsChanged)
      {
        var itemID = itemDefinition.ID;
        Assert.IsNotNull(itemID, "itemID");

        foreach (var file in this.FileMappings)
        {
          Assert.IsNotNull(file, "file");

          if (file.SaveItem(itemID, changes))
          {
            return true;
          }
        }
      }

      return base.SaveItem(itemDefinition, changes, context);
    }

    public override bool ChangeFieldSharing([NotNull] TemplateField fieldDefinition, TemplateFieldSharing sharing, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(fieldDefinition, nameof(fieldDefinition));
      Assert.ArgumentNotNull(context, nameof(context));

      foreach (var mapping in this.FileMappings)
      {
        mapping.ChangeFieldSharing(fieldDefinition.ID, sharing);
      }

      return base.ChangeFieldSharing(fieldDefinition, sharing, context);
    }

    public override bool MoveItem([NotNull] ItemDefinition itemDefinition, [NotNull] ItemDefinition destination, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(destination, nameof(destination));
      Assert.ArgumentNotNull(context, nameof(context));

      var itemID = itemDefinition.ID;
      var targetID = destination.ID;
      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        if (file.MoveItem(itemID, targetID))
        {
          return true;
        }
      }

      return base.MoveItem(itemDefinition, destination, context);
    }

    public override bool RemoveVersion([NotNull] ItemDefinition itemDefinition, [NotNull] VersionUri versionUri, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(versionUri, nameof(versionUri));
      Assert.ArgumentNotNull(context, nameof(context));

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        if (file.RemoveVersion(itemID, versionUri))
        {
          return true;
        }
      }

      return base.RemoveVersion(itemDefinition, versionUri, context);
    }

    public override bool RemoveVersions([NotNull] ItemDefinition itemDefinition, [NotNull] Language language, bool removeSharedData, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(language, nameof(language));
      Assert.ArgumentNotNull(context, nameof(context));

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        if (file.RemoveVersions(itemID, language))
        {
          return true;
        }
      }

      return base.RemoveVersions(itemDefinition, language, removeSharedData, context);
    }

    public override bool DeleteItem([NotNull] ItemDefinition itemDefinition, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(context, nameof(context));

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var file in this.FileMappings)
      {
        Assert.IsNotNull(file, "file");

        if (file.DeleteItem(itemID))
        {
          return true;
        }
      }

      return base.DeleteItem(itemDefinition, context);
    }

    public static void InitializeDefaultValues([NotNull] JsonFields fields)
    {
      Assert.ArgumentNotNull(fields, nameof(fields));

      foreach (var pair in IgnoreFields)
      {
        var fieldID = pair.Key;
        var field = pair.Value;
        if (field == null)
        {
          continue;
        }

        var fieldValue = field.DefaultValue;
        if (field.IsShared)
        {
          fields.Shared[fieldID] = fieldValue;
        }
        else if (field.IsUnversioned)
        {
          foreach (var langGroup in fields.Unversioned)
          {
            langGroup.Value[fieldID] = fieldValue;
          }
        }
        else if (field.IsVersioned)
        {
          foreach (var langGroup in fields.Versioned)
          {
            foreach (var verGroup in langGroup.Value)
            {
              verGroup.Value[fieldID] = fieldValue;
            }
          }
        }
        else
        {
          throw new NotImplementedException();
        }
      }
    }

    private static string GetBlobFilePath(Guid blobId)
    {
      var blob = blobId.ToString();
      var blobFolder = Settings.GetSetting("Media.BlobFolder", "/App_Data/MediaBlobs");
      var virtualPath = Path.Combine(blobFolder, blob.Substring(0, 1), blob.Substring(1, 1), blob.Substring(2, 1), blob + ".bin");
      var physicalPath = MainUtil.MapPath(virtualPath);
      return physicalPath;
    }
  }
}