namespace Sitecore.Data.Helpers
{
  using System.IO;
  using System.Linq;

  using Newtonsoft.Json;

  using Sitecore.Diagnostics;

  public static class JsonHelper
  {
    private static readonly JsonNonPublicMemberContractResolver ContractResolver = new JsonNonPublicMemberContractResolver();

    private static readonly JsonSerializer JsonSerializer = new JsonSerializer() { ContractResolver = ContractResolver };

    [NotNull]
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
      ContractResolver = ContractResolver
    };

    [CanBeNull]
    public static T Deserialize<T>([NotNull] string json)
    {
      Assert.ArgumentNotNull(json, nameof(json));

      return JsonConvert.DeserializeObject<T>(json, JsonSettings);
    }

    [CanBeNull]
    public static T Deserialize<T>([NotNull] TextReader reader)
    {
      Assert.ArgumentNotNull(reader, nameof(reader));

      return JsonSerializer.Deserialize<T>(new JsonTextReader(reader));
    }

    [NotNull]
    public static string Serialize([NotNull] object obj, bool indented)
    {
      Assert.ArgumentNotNull(obj, nameof(obj));

      var json = JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
      Assert.IsNotNull(json, "json");

      return json;
    }

    public static void WriteLineBreak([NotNull] JsonWriter writer)
    {
      Assert.ArgumentNotNull(writer, nameof(writer));

      writer.WriteRaw("\r\n");
      var depths = writer.Path.Count(x => x == '{' || x == '[' || x == '.');
      writer.WriteRaw(string.Concat(Enumerable.Repeat('\t', depths)));
    }
  }
}