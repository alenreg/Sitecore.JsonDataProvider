namespace Sitecore.Pipelines.GetContentEditorWarnings
{
  using System.Linq;
  using System.Web;
  using Sitecore.Collections;
  using Sitecore.StringExtensions;
  using Sitecore.Web.UI.HtmlControls;
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
      if (!JsonDataProvider.Instances.TryGetValue(databaseName, out dataProvider) || dataProvider == null)
      {
        return;
      }

      var mappings = dataProvider.Mappings.OfType<IFileMapping>().ToArray();
      if(mappings.Length == 0)
      {
        return;
      }

      var mapping = mappings.FirstOrDefault(x => x.GetItemDefinition(itemID) != null);
      if (mapping != null)
      {
        var jsonItem = args.Add();
        if (mapping.ReadOnly)
        {
          jsonItem.Title = "JSON Read-Only Item";
          jsonItem.Text = $"This item is stored in {mapping.DisplayName}, but is read-only as configured in item mapping settings.";
        }
        else
        {
          jsonItem.Title = "JSON Item";
          jsonItem.Text = $"This item is stored in {mapping.DisplayName}.";
        }
      }

      var overrideJsonMapping = Registry.GetValue("overrideJsonMapping");
      if (!string.IsNullOrEmpty(overrideJsonMapping))
      {
        mapping = mappings.FirstOrDefault(m => m.DisplayName == HttpUtility.UrlDecode(overrideJsonMapping) && m.AcceptsNewChildrenOf(item.ID));
      }

      if (mapping == null)
      {
        mapping = mappings.FirstOrDefault(m => m.AcceptsNewChildrenOf(item.ID));
      }

      if (mapping == null)
      {
        return;
      }

      var createChildren = args.Add();
      createChildren.Title = "JSON Children";
      createChildren.Text = $"All new children of this item will be stored in <b>{mapping.DisplayName}</b>.";
      if (mappings.Length > 1)
      {
        foreach (IFileMapping otherMapping in mappings)
        {
          if (otherMapping == mapping || otherMapping.ReadOnly)
          {
            continue;
          }

          createChildren.Options.Add(new Pair<string, string>("Change to " + otherMapping.DisplayName, "json:override(id={0})".FormatWith(HttpUtility.UrlEncode(otherMapping.DisplayName))));
        }
      }
    }
  }
}