namespace Sitecore.Data.Helpers
{
  using Newtonsoft.Json;

  using Sitecore.Diagnostics;

  public static class JsonHelper
  {
    [NotNull]
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
      ContractResolver = new JsonNonPublicMemberContractResolver()
    };

    [CanBeNull]
    public static T Deserialize<T>([NotNull] string json)
    {
      Assert.ArgumentNotNull(json, "json");

      return JsonConvert.DeserializeObject<T>(json, JsonSettings);
    }

    [NotNull]
    public static string Serialize([NotNull] object obj, bool indented)
    {
      Assert.ArgumentNotNull(obj, "obj");

      var json = JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
      Assert.IsNotNull(json, "json");

      return json;
    }
  }
}