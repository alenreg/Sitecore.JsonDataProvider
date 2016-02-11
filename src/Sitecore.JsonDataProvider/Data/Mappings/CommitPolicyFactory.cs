using System;

namespace Sitecore.Data.Mappings
{
  public static class CommitPolicyFactory
  {
    public static ICommitPolicy GetCommitPolicy(string intervalText, Action doCommit)
    {
      TimeSpan timeSpan;
      if (string.IsNullOrEmpty(intervalText) || !TimeSpan.TryParse(intervalText, out timeSpan) || timeSpan.Milliseconds < 200) // less than 200ms timeout is too small
      {
        return new ImmediateCommitPolicy(doCommit);
      }

      return new IntervalCommitPolicy(doCommit, timeSpan.Milliseconds);
    }
  }
}