namespace Sitecore.Extensions
{
  using System;

  internal static class Extensions
  {
    internal static T OnlyIf<T>(this T obj, Func<T, bool> func) where T : class
    {
      if (obj == null)
      {
        return null;
      }

      return func(obj) ? obj : null;
    }
  }
}