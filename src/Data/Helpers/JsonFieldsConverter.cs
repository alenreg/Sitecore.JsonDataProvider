namespace Sitecore.Data.Helpers
{
  using System;
  using System.Linq;

  using Newtonsoft.Json;

  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;

  public class JsonFieldsConverter : JsonConverter
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
      var fields = (JsonFields)value;

      var shared = fields.Shared;
      if (shared.Count > 0)
      {
        writer.WritePropertyName("Shared");
        serializer.Serialize(writer, shared);
      }

      var unversioned = fields.Unversioned;
      if (unversioned.Count > 0 && unversioned.Any(x => x.Value.Count > 0))
      {
        writer.WritePropertyName("Unversioned");
        writer.WriteStartObject();
        foreach (var languageGroup in unversioned)
        {
          var fieldsCollection = languageGroup.Value;
          if (fieldsCollection.Count > 0)
          {
            writer.WritePropertyName(languageGroup.Key);
            serializer.Serialize(writer, fieldsCollection);
          }
        }

        writer.WriteEndObject();
      }

      var versioned = fields.Versioned;
      if (versioned.Count > 0 && versioned.Any(x => x.Value.Count > 0))
      {
        writer.WritePropertyName("Versioned");
        writer.WriteStartObject();
        foreach (var languageGroup in versioned)
        {
          if (languageGroup.Value.Count > 0)
          {
            writer.WritePropertyName(languageGroup.Key);
            serializer.Serialize(writer, languageGroup.Value);
          }
        }
        writer.WriteEndObject();
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