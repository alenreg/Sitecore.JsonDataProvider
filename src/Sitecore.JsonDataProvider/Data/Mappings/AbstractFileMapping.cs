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

    private readonly string VirtualPath;

    private readonly ICommitPolicy CommitPolicy;

    protected AbstractFileMapping([NotNull] XmlElement mappingElement, [NotNull] string databaseName)
      : base(mappingElement, databaseName)
    {
      var fileName = mappingElement.GetAttribute("file");
      Assert.IsNotNullOrEmpty(fileName, $"The \"file\" attribute is not specified or has empty string value: {mappingElement.OuterXml}");

      var filePath = MainUtil.MapPath(fileName);
      Assert.IsNotNullOrEmpty(filePath, nameof(filePath));
      
      var media = mappingElement.GetAttribute("media");
      var intervalText = mappingElement.GetAttribute("interval");

      this.VirtualPath = fileName;
      this.FileMappingPath = filePath;
      this.MediaFolderPath = !string.IsNullOrEmpty(media) ? MainUtil.MapPath(media) : null;
      this.CommitPolicy = CommitPolicyFactory.GetCommitPolicy(intervalText, this.DoCommit);
    }
    
    public string MediaFolderPath { get; }

    public string FilePath => this.FileMappingPath;

    public override string DisplayName => string.IsNullOrEmpty(base.Name) ? $"{this.VirtualPath} file" : base.DisplayName;

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
        Lock.EnterWriteLock();
        try
        {
          this.ItemsCache.Clear();
          this.ItemChildren.Clear();
          this.ItemChildren.AddRange(this.Initialize(json));
        }
        finally
        {
          Lock.ExitWriteLock();
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
      this.CommitPolicy.Commit();
    }

    [NotNull]
    protected abstract object GetCommitObject();

    private void DoCommit()
    {
      var filePath = this.FileMappingPath;
      var directory = Path.GetDirectoryName(filePath);
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      Lock.EnterReadLock();
      try
      {
        Log.Info("Saving file: " + filePath, this);

        var json = JsonHelper.Serialize(this.GetCommitObject(), true);
        File.WriteAllText(filePath, json);
      }
      finally
      {
        Lock.ExitReadLock();
      }

      this.GeneratePackageDesignerProject();
    }

    private void GeneratePackageDesignerProject()
    {
      var name = Path.GetFileNameWithoutExtension(this.FileMappingPath);

      Lock.EnterReadLock();
      try
      {
        var items = this.ItemsCache;
        if (JsonDataProvider.Instances[this.DatabaseName].Mappings.Count > 1)
        {
          PackageDesignerHeper.GenerateProject(this.DatabaseName, "auto-generated-for-mapping-" + name, items.Keys);
        }
      }
      finally
      {
        Lock.ExitReadLock();
      }

      foreach (var pair in JsonDataProvider.Instances)
      {
        var databaseName = pair.Key;
        var ids = pair.Value.Mappings.SelectMany(z => z.GetIDs()).Distinct();
        PackageDesignerHeper.GenerateProject(databaseName, "auto-generated-for-database-" + databaseName, ids);
      }
    }
  }
}