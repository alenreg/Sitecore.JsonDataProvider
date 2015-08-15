namespace Sitecore.Support.Data.Composite.Json
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;

  using Newtonsoft.Json;

  using Sitecore.Data;
  using Sitecore.Data.DataProviders;
  using Sitecore.Diagnostics;
  using Sitecore.StringExtensions;
  using Sitecore.Support.Data.DataProviders;

  public class JsonFile : CompositeFile
  {
    [CanBeNull]
    private List<CompositeItem> rootItems;

    [CanBeNull, UsedImplicitly]
    private ID parentID;

    [CanBeNull]
    public string DatabaseName { get; set; }

    [CanBeNull, JsonIgnore]
    public string FilePath { get; set; }

    public override IReadOnlyCollection<CompositeItem> RootItems
    {
      get
      {
        return this.GetRootItems();
      }
    }

    public override ID ParentID
    {
      get
      {
        var value = this.parentID;
        Assert.IsNotNull(value, "value");

        return value;
      }
    }

    [NotNull]
    private IReadOnlyCollection<CompositeItem> GetRootItems()
    {
      var value = this.rootItems;
      if (value != null)
      {
        return value;
      }

      lock (this)
      {
        value = this.rootItems;
        if (value != null)
        {
          return value;
        }

        value = new List<CompositeItem>();
        this.rootItems = value;
        return value;
      }
    }

    [NotNull]
    public static JsonFile Parse([NotNull] string databaseName, [NotNull] string filePath, [NotNull] ID parentId)
    {
      Assert.ArgumentNotNull(databaseName, "databaseName");
      Assert.ArgumentNotNull(filePath, "filePath");
      Assert.ArgumentNotNull(parentId, "parentId");

      if (File.Exists(filePath))
      {
        try
        {
          Log.Info("Reading items from {0}".FormatWith(filePath), typeof(JsonFile));
          var databaseObject = JsonConvert.DeserializeObject<JsonFile>(File.ReadAllText(filePath));
          databaseObject.FilePath = filePath;
          databaseObject.Initialize();
          return databaseObject.Validate();
        }
        catch (Exception ex)
        {
          Log.Error("Failed to load {0} file".FormatWith(filePath), ex, typeof(JsonFile));
        }
      }

      return new JsonFile
        {
          DatabaseName = databaseName,
          FilePath = filePath,
          parentID = parentId
        };
    }

    [NotNull]
    public JsonFile Validate()
    {
      var databaseName = this.DatabaseName;
      if (string.IsNullOrEmpty(databaseName))
      {
        throw new InvalidDataException("The {0} file contains null or emoty in DatabaseName property".FormatWith(this.FilePath));
      }

      var rootItems = this.RootItems;
      if (rootItems == null)
      {
        throw new InvalidDataException("The {0} file contains null in RootItems property".FormatWith(this.FilePath));
      }

      foreach (var rootItem in rootItems)
      {
        if (rootItem == null)
        {
          throw new InvalidDataException("The {0} file contains null in RootItems collection".FormatWith(this.DatabaseName));
        }

        rootItem.Validate(true);
      }

      return this;
    }

    [CanBeNull]
    public JsonItem FindItem([NotNull] ID itemId)
    {
      Assert.ArgumentNotNull(itemId, "itemId");

      // first scan level 1
      var rootItems = this.RootItems;
      var item = rootItems.SingleOrDefault(x => x.Id == itemId);
      if (item != null)
      {
        return item;
      }

      // scan level 2
      foreach (var rootItem in rootItems)
      {
        item = FindItem(rootItem, itemId);
        if (item != null)
        {
          return item;
        }
      }

      return null;
    }

    public override void CreateRootItem(ID itemID, string itemName, ID templateID, CallContext context)
    {

    }

    public override void CreateChildItem(ID itemID, string itemName, CompositeItem parentItem, CallContext context)
    {
    }

    public void Commit()
    {
      var filePath = this.FilePath;
      Assert.IsNotNull(filePath, "filePath");

      try
      {
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        Log.Info("Committing items to the {0} file".FormatWith(filePath), this);
        File.WriteAllText(filePath, JsonConvert.SerializeObject(this, new JsonSerializerSettings { Formatting = Formatting.Indented }));
      }
      catch (Exception ex)
      {
        Log.Error("Failed to commit changes to {0} file".FormatWith(filePath), ex, this);
      }
    }

    [CanBeNull]
    private static JsonItem FindItem([NotNull] JsonItem parentJsonItem, [NotNull] ID itemId)
    {
      Assert.ArgumentNotNull(parentJsonItem, "parentJsonItem");
      Assert.ArgumentNotNull(itemId, "itemId");

      var children = parentJsonItem.Children;
      Assert.IsNotNull(children, "children");

      // scan level x
      var item = children.SingleOrDefault(x => x.Id == itemId);
      if (item != null)
      {
        return item;
      }

      // scan level x+1
      foreach (var rootItem in children)
      {
        item = FindItem(rootItem, itemId);
        if (item != null)
        {
          return item;
        }
      }

      return null;
    }

    /// <summary>
    /// During initialization process the [JsonIgnore]-marked naviation properties are being set.
    /// </summary>
    private void Initialize()
    {
      var rootItems = this.RootItems;
      foreach (var rootItem in rootItems)
      {
        rootItem.Initialize();
      }
    }
  }
}