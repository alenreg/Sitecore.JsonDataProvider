using System;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Sitecore.Data.Mappings
{
  public class IntervalCommitPolicy : ICommitPolicy
  {
    private readonly Action DoCommit;

    private bool CommitRequested;

    public IntervalCommitPolicy(Action doCommit, double interval)
    {
      this.DoCommit = doCommit;

      new Timer
      {
        Enabled = true,
        Interval = interval
      }.Elapsed += TryCommit;
    }

    private void TryCommit(object sender, ElapsedEventArgs e)
    {
      lock (this)
      {
        if (!CommitRequested)
        {
          return;
        }

        DoCommit();

        CommitRequested = false;
      }
    }

    public void Commit()
    {
      lock (this)
      {
        CommitRequested = true;
      }
    }
  }
}