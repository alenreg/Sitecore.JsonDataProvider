namespace Sitecore.Hooks
{
  using Sitecore;
  using Sitecore.Events.Hooks;
  using Sitecore.Resources.Media;

  [UsedImplicitly]
  public class ReplaceMediaCreator : IHook
  {
    public void Initialize()
    {
      MediaManager.Provider.Creator = new JsonMediaCreator();
    }
  }
}