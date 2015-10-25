using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Pipelines.GetContentEditorWarnings
{
  public class FixExclusive
  {
    [UsedImplicitly]
    public void Process(GetContentEditorWarningsArgs args)
    {
      var exclusive = args.Warnings.FirstOrDefault(x => x.IsExclusive);
      if (exclusive != null)
      {
        foreach (var warning in args.Warnings.ToArray())
        {
          if (warning.Title.StartsWith("JSON"))
          {
            continue;
          }

          if (!warning.IsExclusive)
          {
            args.Warnings.Remove(warning);
          }
        }

        exclusive.IsExclusive = false;
      }
    }
  }
}