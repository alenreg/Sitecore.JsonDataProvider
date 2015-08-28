namespace Sitecore.Data.Converters
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;

  using Newtonsoft.Json;

  using Sitecore.Data.Collections;
  using Sitecore.Data.DataProviders;
  using Sitecore.Data.Helpers;
  using Sitecore.Diagnostics;

  public class JsonUnversionedFieldsCollectionConverter : JsonConverter
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

      var any = false;
      writer.WriteStartObject();
      var dictionary = (Dictionary<string, JsonFieldsCollection>)value;
      foreach (var pair in dictionary)
      {
        var fieldsCollection = pair.Value;
        if (fieldsCollection == null || fieldsCollection.Count == 0)
        {
          continue;
        }

        any = true;
        writer.WritePropertyName(pair.Key);
        serializer.Serialize(writer, pair.Value);
      }

      if (JsonDataProvider.BetterMerging)
      {
        if (any)
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