﻿namespace Sitecore.Pipelines.GetContentEditorWarnings
{
  using System.Linq;
  using Sitecore.Data.DataProviders;
  using Sitecore.Data.Mappings;
  using Sitecore.Diagnostics;

  public class JsonItemInfo
  {
    [UsedImplicitly]
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
      if (mapping != null)
      {
        var jsonItem = args.Add();
        if (mapping.ReadOnly)
        {
          jsonItem.Title = "JSON Read-Only Item";
          jsonItem.Text = $"This item is stored in the '{mapping.FilePath}' file, but is read-only as configured in file mapping settings.";
        }
        else
        {
          jsonItem.Title = "JSON Item";
          jsonItem.Text = $"This item is stored in the '{mapping.FilePath}' file.";
        }
      }

      mapping = dataProvider.FileMappings.FirstOrDefault(m => m.AcceptsNewChildrenOf(item.Parent.ID));
      if (mapping == null)
      {
        return;
      }

      var createChildren = args.Add();
      createChildren.Title = "JSON Children";
      createChildren.Text = $"All new children of this item will be stored in the '{mapping.FilePath}' file. ";
    }
  }
}