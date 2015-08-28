namespace Sitecore.Data.Mappings
{
  using System.Collections.Generic;

  using Sitecore.Collections;
  using Sitecore.Data.Items;
  using Sitecore.Globalization;

  public interface IMapping
  {
    void Initialize();

    [CanBeNull]
    IEnumerable<ID> GetChildIDs([NotNull] ID itemId);

    [CanBeNull]
    ItemDefinition GetItemDefinition([NotNull] ID itemID);

    [CanBeNull]
    ID GetParentID([NotNull] ID itemID);

    [CanBeNull]
    VersionUriList GetItemVersiones([NotNull] ID itemID);

    [CanBeNull]
    FieldList GetItemFields([NotNull] ID itemID, [NotNull] VersionUri versionUri);

    [CanBeNull]
    IEnumerable<ID> GetTemplateItemIDs();

    [NotNull]
    IEnumerable<string> GetLanguages();

    bool CreateItem([NotNull] ID itemID, [NotNull] string itemName, [NotNull] ID templateID, [NotNull] ID parentID);

    bool CopyItem([NotNull] ID sourceItemID, [NotNull] ID destinationItemID, [NotNull] ID copyID, [NotNull] string copyName);

    int AddVersion([NotNull] ID itemID, [NotNull] VersionUri versionUri);

    bool SaveItem([NotNull] ID itemID, [NotNull] ItemChanges changes);

    bool MoveItem([NotNull] ID itemID, [NotNull] ID targetID);

    bool RemoveVersion([NotNull] ID itemID, [NotNull] VersionUri versionUri);

    bool RemoveVersions([NotNull] ID itemID, [NotNull] Language language);

    bool DeleteItem([NotNull] ID itemID);
  }
}