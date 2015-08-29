namespace Sitecore.Data.Items
{
  using Newtonsoft.Json;

  using Sitecore.Data;
  using Sitecore.Data.DataProviders;
  using Sitecore.Diagnostics;

  public class JsonItem
  {
    [NotNull]
    [JsonProperty(Order = 1)]
    public readonly ID ID;

    [NotNull]
    [JsonProperty(Order = 4)]
    public readonly JsonFields Fields = new JsonFields();

    [NotNull]
    [JsonProperty(Order = 5)]
    public readonly JsonChildren Children = new JsonChildren();

    [UsedImplicitly]
    public JsonItem()
    {
      this.ID = ID.Null;
      this.ParentID = ID.Null;

      JsonDataProvider.InitializeDefaultValues(this.Fields);
    }

    public JsonItem([NotNull] ID id, [NotNull] ID parentID)
    {
      Assert.ArgumentNotNull(id, "id");
      Assert.ArgumentNotNull(parentID, "parentID");

      this.ID = id;
      this.ParentID = parentID;

      this.Name = "noname";
      this.TemplateID = ID.Null;

      var jsonFields = this.Fields;
      JsonDataProvider.InitializeDefaultValues(jsonFields);
    }

    public JsonItem([NotNull] ID id, [NotNull] ID parentID, [NotNull] JsonChildren children)
      : this(id, parentID)
    {
      Assert.ArgumentNotNull(id, "id");
      Assert.ArgumentNotNull(parentID, "parentID");
      Assert.ArgumentNotNull(children, "children");

      this.Children = children;
    }

    [NotNull]
    [JsonProperty(Order = 2)]
    public string Name { get; set; }

    [NotNull, JsonIgnore]
    public ID ParentID { get; set; }

    [NotNull]
    [JsonProperty(Order = 3)]
    public ID TemplateID { get; set; }
  }
}