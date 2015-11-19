namespace Sitecore.Data.Mappings
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Web.Hosting;
  using System.Xml;

  using Sitecore.Data.DataProviders;
  using Sitecore.Data.Helpers;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;

  public abstract class AbstractFileMapping : AbstractMapping, IFileMapping
  {
    [NotNull]
    public readonly string FileMappingPath;

    protected AbstractFileMapping([NotNull] XmlElement mappingElement, [NotNull] string databaseName)
      : base(mappingElement, databaseName)
    {
      var fileName = mappingElement.GetAttribute("file");
      Assert.IsNotNullOrEmpty(fileName, $"The \"file\" attribute is not specified or has empty string value: {mappingElement.OuterXml}");

      var filePath = MainUtil.MapPath(fileName);
      Assert.IsNotNullOrEmpty(filePath, nameof(filePath));
      
      var media = mappingElement.GetAttribute("media");

      this.FileMappingPath = filePath;
      this.MediaFolderPath = !string.IsNullOrEmpty(media) ? HostingEnvironment.MapPath(media) : null;
    }

    public override string DisplayName => $"the {this.FilePath} file";

    public string MediaFolderPath { get; }

    public string FilePath => this.FileMappingPath;

    public override void Initialize()
    {
      var filePath = this.FileMappingPath;
      if (!File.Exists(filePath))
      {
        return;
      }

      Log.Info($"Deserializing items from: {filePath}", this);
      var json = File.ReadAllText(filePath);

      try
      {
        lock (this.SyncRoot)
        {
          this.ItemsCache.Clear();
          this.ItemChildren.Clear();
          this.ItemChildren.AddRange(this.Initialize(json));
        }

        this.GeneratePackageDesignerProject();
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Cannot deserialize json file: {this.FileMappingPath}", ex);
      }
    }

    [NotNull]
    protected abstract IEnumerable<JsonItem> Initialize([NotNull] string json);

    public override void Commit()
    {
      var filePath = this.FileMappingPath;
      var directory = Path.GetDirectoryName(filePath);
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      var json = JsonHelper.Serialize(this.GetCommitObject(), true);
      File.WriteAllText(filePath, json);

      this.GeneratePackageDesignerProject();
    }

    [NotNull]
    protected abstract object GetCommitObject();

    private void GeneratePackageDesignerProject()
    {
      var name = Path.GetFileNameWithoutExtension(this.FileMappingPath);
      var items = this.ItemsCache;

      if (JsonDataProvider.Instances[this.DatabaseName].Mappings.Count > 1)
      {
        PackageDesignerHeper.GenerateProject(this.DatabaseName, "auto-generated-for-mapping-" + name, items.Select(x => x.ID));
      }

      foreach (var pair in JsonDataProvider.Instances)
      {
        var databaseName = pair.Key;
        var ids = pair.Value.Mappings.SelectMany(z => z.GetAllItemsIDs()).Distinct();
        PackageDesignerHeper.GenerateProject(databaseName, "auto-generated-for-database-" + databaseName, ids);
      }
    }
  }
}