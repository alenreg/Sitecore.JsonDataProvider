using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Pipelines.GetContentEditorWarnings
{
  using Sitecore.Data;
  using Sitecore.Data.Clones;
  using Sitecore.Data.DataProviders;
  using Sitecore.Data.Items;
  using Sitecore.Data.Mappings;
  using Sitecore.Diagnostics;

  public class JsonItemInfo
  {
    public void Process(GetContentEditorWarningsArgs args)
    {
      Assert.ArgumentNotNull(args, nameof(args));

      var item = args.Item;
      Assert.IsNotNull(item, "item to process");

      var databaseName = item.Database.Name;
      var itemID = item.ID;
      JsonDataProvider dataProvider;
      if (!JsonDataProvider.Instances.TryGetValue(databaseName, out dataProvider) || dataProvider == null || dataProvider.FileMappings.Count == 0)
      {
        return;
      }

      var mapping = dataProvider.FileMappings.FirstOrDefault(x => x.GetItemDefinition(itemID) != null);
      if (mapping == null)
      {
        return;
      }

      var warning = args.Add();
      warning.Title = "JSON Item";
      warning.Text = $"This item is stored in the '{mapping.FilePath}' file.";
    }
  }
}