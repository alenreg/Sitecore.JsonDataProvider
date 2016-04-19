namespace Sitecore.Data.Converters
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;
  using Sitecore.Data;
  using Sitecore.Data.Collections;
  using Sitecore.Data.DataProviders;
  using Sitecore.Data.Helpers;
  using Sitecore.Diagnostics;

  public class JsonFieldsCollectionConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType) => false;

    public override void WriteJson([NotNull] JsonWriter writer, [NotNull] object value, [NotNull] JsonSerializer serializer)
    {
      Assert.ArgumentNotNull(writer, nameof(writer));
      Assert.ArgumentNotNull(value, nameof(value));
      Assert.ArgumentNotNull(serializer, nameof(serializer));

      var dictionary = (IDictionary<ID, string>)value;
      writer.WriteStartObject();
      var any = false;
      foreach (var field in dictionary)
      {
        var id = field.Key;
        if (JsonDataProvider.IgnoreFields.ContainsKey(id))
        {
          continue;
        }

        any = true;
        writer.WritePropertyName(id.ToString());
        var fieldValue = field.Value;
        if (JsonDataProvider.Settings.SpecialFields.Enabled && fieldValue.Length < JsonDataProvider.Settings.SpecialFields.MaxFieldLength && fieldValue.Contains('|'))
        {
          writer.WriteStartArray();
          var any1 = false;
          foreach (var arrayItem in fieldValue.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
          {
            if (!string.IsNullOrEmpty(arrayItem))
            {
              any1 = true;
              writer.WriteValue(arrayItem);
            }
          }

          if (JsonDataProvider.Settings.BetterMerging)
          {
            if (any1)
            {
              writer.WriteRaw(",");
            }
          }

          writer.WriteEndArray();
        }
        else
        {
          writer.WriteValue(fieldValue);
        }
      }

      if (JsonDataProvider.Settings.BetterMerging)
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

    [CanBeNull]
    public override object ReadJson([NotNull] JsonReader reader, [CanBeNull] Type objectType, [CanBeNull] object existingValue, [NotNull] JsonSerializer serializer)
    {
      Assert.ArgumentNotNull(reader, nameof(reader));
      Assert.ArgumentNotNull(serializer, nameof(serializer));

      try
      {
        var dictionary = serializer.Deserialize<Dictionary<string, object>>(reader);
        if (dictionary != null)
        {
          return new JsonFieldsCollection(dictionary.ToDictionary(ParseKey, ParseValue));
        }

        return null;
      }
      catch (Exception ex)
      {
        Log.Error("Cannot deserialize " + this.GetType().FullName, ex, this);

        return null;
      }
    }

    private static ID ParseKey(KeyValuePair<string, object> x)
    {
      return ID.Parse(x.Key);
    }

    private static string ParseValue(KeyValuePair<string, object> x)
    {
      return x.Value as string ?? string.Join("|", ((JArray)x.Value).Select(y => (string)y));
    }
  }
}