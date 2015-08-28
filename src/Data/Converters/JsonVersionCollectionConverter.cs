namespace Sitecore.Data.Converters
{
  using System;
  using System.Collections.Generic;

  using Newtonsoft.Json;

  using Sitecore.Data.Collections;
  using Sitecore.Data.DataProviders;
  using Sitecore.Data.Helpers;
  using Sitecore.Diagnostics;

  public class JsonVersionCollectionConverter : JsonConverter
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

      writer.WriteStartObject();
      var dictionary = (Dictionary<int, JsonFieldsCollection>)value;
      foreach (var pair in dictionary)
      {
        writer.WritePropertyName(pair.Key.ToString());
        serializer.Serialize(writer, pair.Value);
      }

      if (JsonDataProvider.BetterMerging)
      {
        if (dictionary.Count > 0)
        {
          writer.WriteRaw(",");
        }
        else
        {
          JsonHelper.WriteLineBreak(writer);
        }
      }

      writer.WriteEndObject();
    }

    [NotNull]
    public override object ReadJson([CanBeNull] JsonReader reader, [CanBeNull] Type objectType, [CanBeNull] object existingValue, [CanBeNull] JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }
  }
}