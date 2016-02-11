using System;

namespace Sitecore.Data.Mappings
{
  public class ImmediateCommitPolicy : ICommitPolicy
  {
    private readonly Action DoCommit;

    public ImmediateCommitPolicy(Action doCommit)
    {
      this.DoCommit = doCommit;
    }

    public void Commit()
    {
      DoCommit();
    }
  }
}