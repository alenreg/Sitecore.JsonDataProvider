namespace Sitecore.Pipelines.GetContentEditorWarnings
{
  using System.Linq;
  using Sitecore.Data.DataProviders;
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