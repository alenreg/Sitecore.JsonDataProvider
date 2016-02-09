using System.Linq;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.HtmlControls;

namespace Sitecore.Commands
{
  public class OverrideJsonMapping : Command
  {
    public override void Execute(CommandContext context)
    {
      var id = context.Parameters["id"];
      Registry.SetValue("overrideJsonMapping", id);

      Context.ClientPage.SendMessage(this, "item:refresh");
    }
  }
}