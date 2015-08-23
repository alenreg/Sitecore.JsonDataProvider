namespace Sitecore.Data.Helpers
{
  internal static class Settings
  {
    public static readonly ID ItemStyleFieldID = ID.Parse("{A791F095-2521-4B4D-BEF9-21DDA221F608}");

    public static readonly string ItemStyleValue = Configuration.Settings.GetSetting("Json.ItemStyle", "color: #780000; border-left: solid 2px #780000 ; padding-left: 5px;");
  }
}