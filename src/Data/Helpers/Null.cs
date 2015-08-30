namespace Sitecore.Data.Helpers
{
  internal static class Null
  {
    public static readonly string String = null;

    public static readonly object Object = null;

    /// <summary>
    /// This almost useless method was added to bypass annoying Visual Studio 2015 warnings for code like 'if(id as object == null)' 
    /// </summary>
    public static bool IsNull(this object obj)
    {
      return obj == null;
    }
  }
}