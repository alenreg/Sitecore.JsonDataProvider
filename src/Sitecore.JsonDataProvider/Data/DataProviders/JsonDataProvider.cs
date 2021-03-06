﻿namespace Sitecore.Data.DataProviders
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using System.Xml;

  using Sitecore.Collections;
  using Sitecore.Data;
  using Sitecore.Data.Helpers;
  using Sitecore.Data.Items;
  using Sitecore.Data.Mappings;
  using Sitecore.Data.Templates;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.StringExtensions;
  using Sitecore.Web.UI.HtmlControls;

  [UsedImplicitly]
  public class JsonDataProvider : DataProvider
  {
    [NotNull]
    public static readonly Dictionary<string, Type> MappingTypes = new Dictionary<string, Type>();

    [NotNull]
    public static readonly IDictionary<ID, DefaultFieldValue> IgnoreFields = new Dictionary<ID, DefaultFieldValue>();

    [NotNull]
    public readonly IList<IMapping> Mappings = new List<IMapping>();

    [NotNull]
    public readonly string DatabaseName;

    [NotNull]
    private static readonly Dictionary<string, JsonDataProvider> instances = new Dictionary<string, JsonDataProvider>();

    [NotNull]
    public static IReadOnlyDictionary<string, JsonDataProvider> Instances => instances;

    static JsonDataProvider()
    {
      AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
          var currentAssembly = typeof(JsonDataProvider).Assembly;
          var assemblyName = args.Name.Trim();
          var pos = assemblyName.IndexOf(',');
          if (pos >= 0)
          {
            assemblyName = assemblyName.Substring(0, pos).Trim();
          }

          var resourceName = $"Sitecore.Properties.EmbeddedResources.{assemblyName}.dll";
          var stream = currentAssembly.GetManifestResourceStream(resourceName);
          if (stream == null)
          {
            return null;
          }

          var bytes = new byte[stream.Length];
          stream.Read(bytes, 0, bytes.Length);
          return Assembly.Load(bytes);
        };
    }

    public JsonDataProvider([NotNull] string databaseName)
    {
      Assert.ArgumentNotNull(databaseName, nameof(databaseName));

      lock (instances)
      {
        instances[databaseName] = this;
      }

      Log.Info($"JsonDataProvider is being initialized for \"{databaseName}\" database", this);

      this.DatabaseName = databaseName;
    }
    
    [UsedImplicitly]
    public void AddMappingType([NotNull] XmlNode mappingTypeNode)
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
    public void AddMapping([NotNull] XmlNode mappingNode)
    {
      Assert.ArgumentNotNull(mappingNode, nameof(mappingNode));

      var mappingElement = (XmlElement)mappingNode;
      var mappingName = mappingNode.Name;
      Assert.IsNotNullOrEmpty(mappingName, "mappingName");

      Type mappingType;
      MappingTypes.TryGetValue(mappingName, out mappingType);
      Assert.IsNotNull(mappingType, "The {0} mapping type is not registered in <MappingTypes> element".FormatWith(mappingName));

      var mapping = (IMapping)Activator.CreateInstance(mappingType, mappingElement, this.DatabaseName);
      Assert.IsNotNull(mapping, "mapping");

      Log.Info("Mapping is estabileshed: " + mappingName, this);
      this.Mappings.Insert(0, mapping);
      mapping.Initialize();
    }

    [NotNull]
    public override IDList GetChildIDs([NotNull] ItemDefinition itemDefinition, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(context, nameof(context));

      var itemId = itemDefinition.ID;
      Assert.IsNotNull(itemId, "itemId");

      return this.GetChildIDsInternal(itemId);
    }

    public override IDList SelectIDs(string query, CallContext context)
    {
      if (query.StartsWith("fast:", StringComparison.InvariantCulture))
      {
        Log.SingleWarn("JsonDataProvider does not support fast query. Query: " + query, this);

        return new IDList();
      }

      return this.QueryAny(query, context) ?? this.QueryPath(query, context);
    }

    public override ID SelectSingleID(string query, CallContext context)
    {
      if (query.StartsWith("fast:", StringComparison.InvariantCulture))
      {
        Log.SingleWarn("JsonDataProvider does not support fast query. Query: " + query, this);

        return null;
      }

      var idList = this.QueryAny(query, context) ?? this.QueryPath(query, context);
      if (idList == null || idList.Count == 0)
      {
        return null;
      }

      return idList[0];
    }

    [CanBeNull]
    public override ItemDefinition GetItemDefinition([NotNull] ID itemID, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(context, nameof(context));

      return Abort(context, this.GetItemDefinitionInternal(itemID));
    }

    [CanBeNull]
    public override ID GetParentID([NotNull] ItemDefinition itemDefinition, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(context, nameof(context));

      return Abort(context, this.GetParentIDInternal(itemDefinition));
    }

    [CanBeNull]
    public override VersionUriList GetItemVersions([NotNull] ItemDefinition itemDefinition, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(context, nameof(context));

      return Abort(context, this.GetItemVersionsInternal(itemDefinition));
    }

    [NotNull]
    public override FieldList GetItemFields([NotNull] ItemDefinition itemDefinition, [NotNull] VersionUri versionUri, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(versionUri, nameof(versionUri));
      Assert.ArgumentNotNull(context, nameof(context));

      return Abort(context, this.GetItemFieldsInternal(itemDefinition, versionUri));
    }

    [NotNull]
    public override IdCollection GetTemplateItemIds([NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(context, nameof(context));

      return this.TemplateItemIDsInternal();
    }

    [NotNull]
    public override LanguageCollection GetLanguages([NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(context, nameof(context));

      return this.GetLanguagesInternal();
    }

    public override bool CreateItem([NotNull] ID itemID, [NotNull] string itemName, [NotNull] ID templateID, [NotNull] ItemDefinition parent, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(itemName, nameof(itemName));
      Assert.ArgumentNotNull(templateID, nameof(templateID));
      Assert.ArgumentNotNull(parent, nameof(parent));
      Assert.ArgumentNotNull(context, nameof(context));

      return Abort(context, this.CreateItemInternal(itemID, itemName, templateID, parent));
    }

    public override bool CopyItem([NotNull] ItemDefinition source, [NotNull] ItemDefinition destination, [NotNull] string copyName, [NotNull] ID copyID, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(source, nameof(source));
      Assert.ArgumentNotNull(destination, nameof(destination));
      Assert.ArgumentNotNull(copyName, nameof(copyName));
      Assert.ArgumentNotNull(copyID, nameof(copyID));
      Assert.ArgumentNotNull(context, nameof(context));

      return Abort(context, this.CopyItemInternal(source, destination, copyName, copyID, context));
    }

    public override int AddVersion([NotNull] ItemDefinition itemDefinition, [NotNull] VersionUri baseVersion, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(baseVersion, nameof(baseVersion));
      Assert.ArgumentNotNull(context, nameof(context));

      return Abort(context, this.AddVersionInternal(itemDefinition, baseVersion));
    }

    public override bool SaveItem([NotNull] ItemDefinition itemDefinition, [NotNull] ItemChanges changes, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(changes, nameof(changes));
      Assert.ArgumentNotNull(context, nameof(context));

      return Abort(context, this.SaveItemInternal(itemDefinition, changes));
    }

    public override bool ChangeFieldSharing([NotNull] TemplateField fieldDefinition, TemplateFieldSharing sharing, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(fieldDefinition, nameof(fieldDefinition));
      Assert.ArgumentNotNull(context, nameof(context));

      foreach (var mapping in this.Mappings)
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

      return Abort(context, this.MoveItemInternal(itemDefinition, destination));
    }

    public override bool RemoveVersion([NotNull] ItemDefinition itemDefinition, [NotNull] VersionUri versionUri, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(versionUri, nameof(versionUri));
      Assert.ArgumentNotNull(context, nameof(context));

      return Abort(context, this.RemoveVersionInternal(itemDefinition, versionUri));
    }

    public override bool RemoveVersions([NotNull] ItemDefinition itemDefinition, [NotNull] Language language, bool removeSharedData, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(language, nameof(language));
      Assert.ArgumentNotNull(context, nameof(context));

      return Abort(context, this.RemoveVersionsInternal(itemDefinition, language, removeSharedData));
    }

    public override bool DeleteItem([NotNull] ItemDefinition itemDefinition, [NotNull] CallContext context)
    {
      Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
      Assert.ArgumentNotNull(context, nameof(context));

      return Abort(context, this.DeleteItemInternal(itemDefinition));
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

    private IMapping GetOverriddenMapping([NotNull] ID parentID)
    {
      Assert.ArgumentNotNull(parentID, "parentID");

      IMapping mapping1 = null;
      var overrideJsonMapping = Registry.GetValue("overrideJsonMapping");
      if (!string.IsNullOrEmpty(overrideJsonMapping))
      {
        mapping1 =
          this.Mappings.FirstOrDefault(
            m => m.DisplayName == overrideJsonMapping && m.AcceptsNewChildrenOf(parentID));
      }
      return mapping1;
    }

    private IDList QueryPath(string query, CallContext context)
    {
      if (query.IndexOf("//", StringComparison.InvariantCulture) < 0 && query.IndexOf('[') < 0 && query.IndexOf('@') < 0)
        return this.ResolvePaths(query, context);

      return null;
    }

    private IDList QueryAny(string query, CallContext context)
    {
      if (query.IndexOf("//", StringComparison.InvariantCulture) == 0 && query.IndexOf('[') < 0 && query.IndexOf('@') < 0 && query.LastIndexOf('/') == 1)
        return this.ResolveNames(query.Substring(2));

      return null;
    }

    private IDList ResolveNames(string itemName)
    {
      if (ID.IsID(itemName))
      {
        return IDList.Build(ID.Parse(itemName));
      }

      var idList = new IDList();
      foreach (var mapping in this.Mappings)
      {
        var ids = mapping.ResolveNames(itemName);
        if (ids == null)
        {
          continue;
        }

        foreach (var id in ids)
        {
          idList.Add(id);
        }
      }

      return idList;
    }

    private IDList ResolvePaths(string itemPath, CallContext context)
    {
      if (ID.IsID(itemPath))
      {
        return IDList.Build(ID.Parse(itemPath));
      }

      var idList = new IDList();
      foreach (string path in itemPath.Split('|'))
      {
        foreach (var mapping in this.Mappings)
        {
          var ids = mapping.ResolvePath(path, context);
          if (ids == null)
          {
            continue;
          }

          foreach (var id in ids)
          {
            idList.Add(id);
          }
        }
      }

      return idList;
    }

    private IDList GetChildIDsInternal(ID itemId)
    {
      var childIDs = new IDList();

      // several mappings can point to same item so let's collect items from all of them
      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        var fileChildIDs = mapping.GetChildIDs(itemId);
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
      return childIDs;
    }

    private ItemDefinition GetItemDefinitionInternal(ID itemID)
    {
      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        var definition = mapping.GetItemDefinition(itemID);
        if (definition != null)
        {
          return definition;
        }
      }

      return null;
    }

    private ID GetParentIDInternal(ItemDefinition itemDefinition)
    {
      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        var parentID = mapping.GetParentID(itemID);
        if (!Equals(parentID, null))
        {
          return parentID;
        }
      }

      return null;
    }

    private VersionUriList GetItemVersionsInternal(ItemDefinition itemDefinition)
    {
      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        var itemVersions = mapping.GetItemVersions(itemID);
        if (itemVersions != null)
        {
          return itemVersions;
        }
      }

      return null;
    }

    private FieldList GetItemFieldsInternal(ItemDefinition itemDefinition, VersionUri versionUri)
    {
      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        var fieldList = mapping.GetItemFields(itemID, versionUri);
        if (fieldList != null)
        {
          return fieldList;
        }
      }

      return null;
    }

    private IdCollection TemplateItemIDsInternal()
    {
      var templateItemIDs = new IdCollection();
      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        var fileTemplateIDs = mapping.GetTemplateItemIDs();
        if (fileTemplateIDs != null)
        {
          foreach (var templateID in fileTemplateIDs)
          {
            templateItemIDs.Add(templateID);
          }
        }
      }
      return templateItemIDs;
    }

    private LanguageCollection GetLanguagesInternal()
    {
      var languages = new LanguageCollection();
      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        var jsonLanguages = mapping.GetLanguages();
        foreach (var jsonLanguage in jsonLanguages)
        {
          var language = Language.Parse(jsonLanguage.Item1);
          if (!languages.Contains(language))
          {
            language.Origin.ItemId = jsonLanguage.Item2;
            languages.Add(language);
          }
        }
      }
      return languages;
    }

    private bool CreateItemInternal(ID itemID, string itemName, ID templateID, ItemDefinition parent)
    {
      var parentId = parent.ID;
      Assert.IsNotNull(itemID, "itemID");

      var overriddenMapping = GetOverriddenMapping(parent.ID);
      if (overriddenMapping != null && !overriddenMapping.ReadOnly)
      {
        if (overriddenMapping.CreateItem(itemID, itemName, templateID, parentId))
        {
          return true;
        }
      }

      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        if (mapping.ReadOnly)
        {
          continue;
        }

        if (mapping.CreateItem(itemID, itemName, templateID, parentId))
        {
          return true;
        }
      }

      return false;
    }

    private bool CopyItemInternal(ItemDefinition source, ItemDefinition destination, string copyName, ID copyID, CallContext context)
    {
      var sourceItemID = source.ID;
      Assert.IsNotNull(sourceItemID, "sourceItemID");

      var destinationItemID = destination.ID;
      Assert.IsNotNull(destinationItemID, "destinationItemID");

      var overriddenMapping = GetOverriddenMapping(destinationItemID);
      if (overriddenMapping != null && !overriddenMapping.ReadOnly)
      {
        if (overriddenMapping.CopyItem(sourceItemID, destinationItemID, copyID, copyName, context))
        {
          return true;
        }
      }

      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        if (mapping.ReadOnly)
        {
          continue;
        }

        if (mapping.CopyItem(sourceItemID, destinationItemID, copyID, copyName, context))
        {
          return true;
        }
      }

      return false;
    }

    private int AddVersionInternal(ItemDefinition itemDefinition, VersionUri baseVersion)
    {
      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        if (mapping.ReadOnly)
        {
          continue;
        }

        var versionNumber = mapping.AddVersion(itemID, baseVersion);
        if (versionNumber != -1)
        {
          return versionNumber;
        }
      }

      return -1;
    }

    private bool SaveItemInternal(ItemDefinition itemDefinition, ItemChanges changes)
    {
      if (!changes.HasPropertiesChanged && !changes.HasFieldsChanged)
      {
        return false;
      }

      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        if (mapping.ReadOnly)
        {
          continue;
        }

        if (mapping.SaveItem(itemID, changes))
        {
          return true;
        }
      }

      return false;
    }

    private bool MoveItemInternal(ItemDefinition itemDefinition, ItemDefinition destination)
    {
      var itemID = itemDefinition.ID;
      var targetID = destination.ID;
      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        if (mapping.ReadOnly)
        {
          continue;
        }

        if (mapping.MoveItem(itemID, targetID))
        {
          return true;
        }
      }

      return false;
    }

    private bool RemoveVersionInternal(ItemDefinition itemDefinition, VersionUri versionUri)
    {
      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        if (mapping.ReadOnly)
        {
          continue;
        }

        if (mapping.RemoveVersion(itemID, versionUri))
        {
          return true;
        }
      }

      return false;
    }

    private bool RemoveVersionsInternal(ItemDefinition itemDefinition, Language language, bool removeSharedData)
    {
      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        if (mapping.ReadOnly)
        {
          continue;
        }

        if (mapping.RemoveVersions(itemID, language))
        {
          return true;
        }
      }

      return false;
    }

    private bool DeleteItemInternal(ItemDefinition itemDefinition)
    {
      var itemID = itemDefinition.ID;
      Assert.IsNotNull(itemID, "itemID");

      foreach (var mapping in this.Mappings)
      {
        Assert.IsNotNull(mapping, nameof(mapping));

        if (mapping.ReadOnly)
        {
          continue;
        }

        if (mapping.DeleteItem(itemID))
        {
          return true;
        }
      }

      return false;
    }

    private static T Abort<T>(CallContext context, T obj) where T : class
    {
      if (obj == null)
      {
        return null;
      }

      context.Abort();
      return obj;
    }

    private static int Abort(CallContext context, int obj)
    {
      if (obj == -1)
      {
        return -1;
      }

      context.Abort();
      return obj;
    }

    private static bool Abort(CallContext context, bool result)
    {
      if (!result)
      {
        return false;
      }

      context.Abort();
      return true;
    }

    public static class Settings
    {
      public static readonly bool BetterMerging = Configuration.Settings.GetBoolSetting("JSON.BetterMerging", true);

      public static class SpecialFields
      {
        public static readonly bool Enabled = Configuration.Settings.GetBoolSetting("JSON.SpecialFields.Enabled", false);

        public static readonly int MaxFieldLength = Configuration.Settings.GetIntSetting("JSON.SpecialFields.MaxFieldLength", 1024);
      }
    }
  }
}