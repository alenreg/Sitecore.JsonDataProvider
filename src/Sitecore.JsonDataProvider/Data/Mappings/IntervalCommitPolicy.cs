using System;
using System.Timers;
using System.Web.Hosting;
using Timer = System.Timers.Timer;

namespace Sitecore.Data.Mappings
{
  public class IntervalCommitPolicy : ICommitPolicy, IRegisteredObject
  {
    private readonly Action DoCommit;

    private bool CommitRequested;

    public IntervalCommitPolicy(Action doCommit, double interval)
    {
      this.DoCommit = doCommit;

      HostingEnvironment.RegisterObject(this);

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

    public void Stop(bool immediate)
    {
      if (!immediate)
      {
        TryCommit(null, null);
      }
    }
  }
}