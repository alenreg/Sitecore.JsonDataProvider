namespace Sitecore.Data.Helpers
{
  using System.Linq;

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

    public static void WriteLineBreak([NotNull] JsonWriter writer)
    {
      Assert.ArgumentNotNull(writer, "writer");

      writer.WriteRaw("\r\n");
      var depths = writer.Path.Count(x => x == '{' || x == '[' || x == '.');
      writer.WriteRaw(string.Concat(Enumerable.Repeat('\t', depths)));
    }
  }
}