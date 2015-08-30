namespace Sitecore.Data.Helpers
{
  using System;
  using System.Xml;

  using Sitecore.Diagnostics;

  public class DefaultFieldValue
  {
    [CanBeNull]
    public string DefaultValuePattern { get; set; }

    public bool IsShared { get; private set; }

    public bool IsUnversioned { get; private set; }

    public bool IsVersioned { get; private set; }

    [NotNull]
    public string DefaultValue => this.DefaultValuePattern
      .Replace("$(now)", DateUtil.IsoNow)
      .Replace("$(guid)", Guid.NewGuid().ToString());

    [CanBeNull]
    public static DefaultFieldValue Parse([NotNull] XmlElement fieldElement)
    {
      Assert.ArgumentNotNull(fieldElement, nameof(fieldElement));

      var defaultValue = fieldElement.Attributes["defaultValue"];
      if (defaultValue == null)
      {
        return null;
      }

      var type = fieldElement.GetAttribute("type").ToLower();
      return new DefaultFieldValue
        {
          DefaultValuePattern = defaultValue.Value,
          IsShared = string.IsNullOrEmpty(type) || type == "shared",
          IsUnversioned = type == "unversioned",
          IsVersioned = type == "versioned"
        };
    }
  }
}