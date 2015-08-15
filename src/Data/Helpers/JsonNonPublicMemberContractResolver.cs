namespace Sitecore.Support.Data.Helpers
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;

  using Newtonsoft.Json;
  using Newtonsoft.Json.Serialization;

  using Sitecore.Diagnostics;

  public class JsonNonPublicMemberContractResolver : DefaultContractResolver
  {
    [NotNull]
    protected override List<MemberInfo> GetSerializableMembers([NotNull] Type objectType)
    {
      Assert.ArgumentNotNull(objectType, "objectType");

      var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

      return objectType
        .GetProperties(flags).Where(propInfo => propInfo.CanWrite)
        .Concat(objectType.GetFields(flags).Cast<MemberInfo>())
        .OrderBy(GetOrder)
        .ToList();
    }

    [CanBeNull]
    protected override IList<JsonProperty> CreateProperties([CanBeNull] Type type, MemberSerialization memberSerialization)
    {
      return base.CreateProperties(type, MemberSerialization.Fields);
    }

    private static int GetOrder([NotNull] MemberInfo memberInfo)
    {
      Assert.ArgumentNotNull(memberInfo, "memberInfo");

      var attribute = memberInfo.GetCustomAttributes().OfType<JsonPropertyAttribute>().FirstOrDefault();
      if (attribute == null)
      {
        return 0;
      }

      return attribute.Order;
    }
  }
}