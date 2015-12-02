namespace Sitecore.Resources.Media
{
  using System.IO;
  using System.Linq;

  using Sitecore;
  using Sitecore.Data;
  using Sitecore.Data.DataProviders;
  using Sitecore.Data.Mappings;
  using Sitecore.Diagnostics;
  using Sitecore.IO;
  using Sitecore.SecurityModel;

  public class JsonMediaCreator : MediaCreator
  {
    protected override string GetFullFilePath(ID itemID, string fileName, string itemPath, MediaCreatorOptions options)
    {
      Assert.ArgumentNotNull(itemID, nameof(itemID));
      Assert.ArgumentNotNull(fileName, nameof(fileName));
      Assert.ArgumentNotNull(itemPath, nameof(itemPath));
      Assert.ArgumentNotNull(options, nameof(options));

      using (new SecurityDisabler())
      {
        var parentPath = itemPath.Substring(0, itemPath.LastIndexOf('/'));
        var parentId = parentPath.Substring(parentPath.LastIndexOf('/') + 1);
        var database = options.Database ?? Context.ContentDatabase ?? Context.Database;
        if (database == null)
        {
          return base.GetFullFilePath(itemID, fileName, itemPath, options);
        }

        var provider = JsonDataProvider.Instances[database.Name];
        if (provider == null)
        {
          return base.GetFullFilePath(itemID, fileName, itemPath, options);
        }

        ID id;
        if (!ID.TryParse(parentId, out id))
        {
          return base.GetFullFilePath(itemID, fileName, itemPath, options);
        }

        foreach (var fileMapping in provider.Mappings.OfType<IFileMapping>())
        {
          if (fileMapping.ReadOnly)
          {
            continue;
          }

          var mediaFolderPath = fileMapping.MediaFolderPath;
          if (string.IsNullOrEmpty(mediaFolderPath))
          {
            continue;
          }
          
          if (!fileMapping.AcceptsNewChildrenOf(id))
          {
            continue;
          }

          var item = database.GetItem(itemID);
          Assert.IsNotNull(item, "item");

          itemPath = item.Parent.Paths.FullPath;
          var folder = Path.Combine(mediaFolderPath, itemPath.Substring("/sitecore/media library".Length).TrimStart('/'));
          if (!Directory.Exists(folder))
          {
            Directory.CreateDirectory(folder);
          }

          return FileUtil.GetUniqueFilename(Path.Combine(folder, Path.GetFileName(fileName)));
        }

        return base.GetFullFilePath(itemID, fileName, itemPath, options);
      }
    }
  }
}