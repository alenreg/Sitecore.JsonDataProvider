using Sitecore.Diagnostics;

namespace Sitecore.Data.Helpers
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;

  using Sitecore.Configuration;
  using Sitecore.Data.Items;

  public static class PackageDesignerHeper
  {
    public static void GenerateProject(string database, string name, IEnumerable<ID> items)
    {
      var directoryPath = MainUtil.MapPath(Settings.PackagePath);
      var filePath = Path.Combine(directoryPath, name + ".xml");

      try
      {
        if (!Directory.Exists(directoryPath))
        {
          Directory.CreateDirectory(directoryPath);
        }

        var xitems = items.Select(x => $"        <x-item>/{database}/{x}/invariant/0</x-item>");
        File.WriteAllText(filePath, $@"<project>
  <Sources>
    <xitems>
      <Entries>{Environment.NewLine + string.Join(Environment.NewLine, xitems)}
      </Entries>
    </xitems>
  </Sources>
</project>
");
      }
      catch (Exception ex)
      {
        Log.Error($"Failed to generate package project: {filePath}", ex, typeof(PackageDesignerHeper));
      }
    }
  }
}