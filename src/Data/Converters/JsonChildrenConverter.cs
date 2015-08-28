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
    public override bool CanRead
    {
      get
      {
        return false;
      }
    }

    public override bool CanConvert([CanBeNull] Type objectType)
    {
      return false;
    }

    public override void WriteJson([NotNull] JsonWriter writer, [NotNull] object value, [NotNull] JsonSerializer serializer)
    {
      Assert.ArgumentNotNull(writer, "writer");
      Assert.ArgumentNotNull(value, "value");
      Assert.ArgumentNotNull(serializer, "serializer");

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

    public override object ReadJson([CanBeNull] JsonReader reader, [CanBeNull] Type objectType, [CanBeNull] object existingValue, [CanBeNull] JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }
  }
}