namespace Sitecore.Support.Data.Composite.Json
{
  using System.Collections.Generic;
  using System.Linq;

  using Sitecore.Diagnostics;
  using Sitecore.Support.Data.DataProviders;

  public class JsonVersion : CompositeItemVersion
  {
    [CanBeNull]
    private IDictionary<string, string> fields;

    [UsedImplicitly]
    private int number;

    public JsonVersion()
    {
    }

    public JsonVersion(int newNumber)
    {
      this.number = newNumber;
    }

    public JsonVersion(int newNumber, [NotNull] IDictionary<string, string> fields)
    {
      Assert.ArgumentNotNull(fields, "fields");

      this.number = newNumber;
      this.fields = fields;
    }

    public override int Number 
    {
      get
      {
        return this.number;
      }
    }

    public override IDictionary<string, string> Fields
    {
      get
      {
        return this.GetFields();
      }
    }

    public override CompositeItemVersion CreateFrom(CompositeItemVersion version, int newNumber)
    {
      Assert.ArgumentNotNull(version, "version");

      var newFields = version.Fields.ToDictionary(x => x.Key, x => x.Value);
      var key = FieldIDs.WorkflowState.ToString();
      if (newFields.ContainsKey(key))
      {
        newFields.Remove(key);
      }

      return new JsonVersion(newNumber, newFields);
    }

    [NotNull]
    private IDictionary<string, string> GetFields()
    {
      var value = this.fields;
      if (value != null)
      {
        return value;
      }

      lock (this)
      {
        return this.fields = new Dictionary<string, string>();
      }
    }
  }
}