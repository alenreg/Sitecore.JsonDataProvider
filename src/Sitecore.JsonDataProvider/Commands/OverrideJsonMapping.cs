using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.HtmlControls;

namespace Sitecore.Commands
{
  public class OverrideJsonMapping : Command
  {
    public override void Execute(CommandContext context)
    {
      if (context.Parameters["action"] == "reset")
      {
        Registry.SetValue("overrideJsonMapping", string.Empty);
      }
      else
      {
        var id = context.Parameters["id"];
        Assert.IsNotNull(id, "id");

        Registry.SetValue("overrideJsonMapping", id);
      }

      Context.ClientPage.SendMessage(this, "item:refresh");
    }
  }
}