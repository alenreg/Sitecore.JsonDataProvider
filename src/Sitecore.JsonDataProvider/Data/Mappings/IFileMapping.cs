namespace Sitecore.Data.Mappings
{
  public interface IFileMapping : IMapping
  {
    [NotNull]
    string FilePath { get; }

    [CanBeNull]
    string MediaFolderPath { get; }
  }
}