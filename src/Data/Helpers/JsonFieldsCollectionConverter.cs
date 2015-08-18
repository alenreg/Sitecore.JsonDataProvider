namespace Sitecore.Data.Helpers
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  using Newtonsoft.Json;

  using Sitecore.Data;
  using Sitecore.Data.Collections;
  using Sitecore.Data.DataProviders;
  using Sitecore.Diagnostics;

  public class JsonFieldsCollectionConverter : JsonConverter
  {
    public override void WriteJson([NotNull] JsonWriter writer, [NotNull] object value, [NotNull] JsonSerializer serializer)
    {
      Assert.ArgumentNotNull(writer, "writer");
      Assert.ArgumentNotNull(value, "value");
      Assert.ArgumentNotNull(serializer, "serializer");

      var dictionary = (IDictionary<ID, string>)value;
      writer.WriteStartObject();
      foreach (var field in dictionary)
      {
        var id = field.Key;
        if (JsonDataProvider.IgnoreFields.Any(x => x == id))
        {
          continue;
        }

        writer.WritePropertyName(id.ToString());
        writer.WriteValue(field.Value);
      }

      writer.WriteEndObject();
    }

    [CanBeNull]
    public override object ReadJson([NotNull] JsonReader reader, [CanBeNull] Type objectType, [CanBeNull] object existingValue, [NotNull] JsonSerializer serializer)
    {
      Assert.ArgumentNotNull(reader, "reader");
      Assert.ArgumentNotNull(serializer, "serializer");

      try
      {
        var dictionary = serializer.Deserialize<Dictionary<string, string>>(reader);
        if (dictionary != null)
        {
          return new JsonFieldsCollection(dictionary.ToDictionary(x => ID.Parse(x.Key), x => x.Value));
        }

        return null;
      }
      catch (Exception ex)
      {
        Log.Error("Cannot deserialize " + this.GetType().FullName, ex, this);

        return null;
      }
    }

    public override bool CanConvert([CanBeNull] Type objectType)
    {
      return false;
    }
  }
}