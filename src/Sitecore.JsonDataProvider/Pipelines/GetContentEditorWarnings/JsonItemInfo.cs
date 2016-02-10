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
      if (mappings.Length == 0)
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
          jsonItem.Text = $"This item is stored in <b>{mapping.DisplayName}</b>, but is read-only as configured in item mapping settings.";
        }
        else
        {
          jsonItem.Title = "JSON Item";
          jsonItem.Text = $"This item is stored in <b>{mapping.DisplayName}</b>.";
        }
      }

      IFileMapping overrideMapping = null;
      var overrideJsonMapping = Registry.GetValue("overrideJsonMapping");
      if (!string.IsNullOrEmpty(overrideJsonMapping))
      {
        mapping = mappings.FirstOrDefault(m => m.DisplayName == overrideJsonMapping && m.AcceptsNewChildrenOf(item.ID));
        overrideMapping = mapping;
      }

      var firstMapping = mappings.FirstOrDefault(m => m.AcceptsNewChildrenOf(item.ID));
      mapping = mapping ?? firstMapping;
      
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
          if (otherMapping == mapping || !otherMapping.AcceptsNewChildrenOf(itemID))
          {
            continue;
          }

          var reset = overrideMapping != null && firstMapping == otherMapping;
          if (reset)
          {
            createChildren.Options.Add(new Pair<string, string>("Change to " + otherMapping.DisplayName + " (remove override)",
              "json:override(action=reset)".FormatWith(HttpUtility.UrlEncode(otherMapping.DisplayName))));
          }
          else
          {
            createChildren.Options.Add(new Pair<string, string>("Change to " + otherMapping.DisplayName, "json:override(id={0})".FormatWith(HttpUtility.UrlEncode(otherMapping.DisplayName))));
          }
        }
      }
    }
  }
}