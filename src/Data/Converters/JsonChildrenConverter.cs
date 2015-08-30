namespace Sitecore.Data.Converters
{
  using System;

  using Newtonsoft.Json;

  using Sitecore.Data.DataProviders;
  using Sitecore.Data.Helpers;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;

  public class JsonChildrenConverter : JsonConverter
  {
    public override bool CanRead => false;

    public override bool CanConvert(Type objectType) => false;

    public override void WriteJson([NotNull] JsonWriter writer, [NotNull] object value, [NotNull] JsonSerializer serializer)
    {
      Assert.ArgumentNotNull(writer, nameof(writer));
      Assert.ArgumentNotNull(value, nameof(value));
      Assert.ArgumentNotNull(serializer, nameof(serializer));

      writer.WriteStartArray();

      var array = (JsonChildren)value;
      foreach (var jsonItem in array)
      {
        serializer.Serialize(writer, jsonItem);
      }

      if (JsonDataProvider.BetterMerging)
      {
        if (array.Count > 0)
        {
          writer.WriteRaw(",");
        }
        else
        {
          JsonHelper.WriteLineBreak(writer);
        }
      }

      writer.WriteEndArray();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => Throw.NotImplementedException();
  }
}