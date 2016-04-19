using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Mappings;
using Sitecore.Diagnostics;
using Sitecore.Jobs;

namespace Sitecore.Hooks
{
  using Sitecore.Events.Hooks;

  public class CheckDuplicateIDs : IHook
  {
    public void Initialize()
    {
      JobManager.Start(new JobOptions("Check duplicate IDs", "JsonDataProvider", "scheduler", this, "Run"));
    }

    public void Run()
    {
      Log.Info("JsonDataProvider mappings are being validated.", this);
      var list = new List<ID>();
      var stopwatch = Stopwatch.StartNew();
      foreach (var database in Factory.GetDatabases())
      {
        var providers = database.GetDataProviders().OfType<JsonDataProvider>().ToArray();
        foreach (var provider1 in providers)
        {
          var mappings1 = provider1.Mappings.OfType<AbstractMapping>().ToArray();
          foreach (var mapping1 in mappings1)
          {
            foreach (var mapping2 in mappings1)
            {
              if (mapping1 == mapping2)
              {
                continue;
              }

              Validate(list, mapping1, mapping2);
            }

            foreach (var provider2 in providers)
            {
              if (provider1 == provider2)
              {
                continue;
              }

              foreach (var mapping2 in provider2.Mappings.OfType<AbstractMapping>())
              {
                Validate(list, mapping1, mapping2);
              }
            }
          }
        }
      }

      if (list.Count > 0)
      {
        foreach (var id in list.Distinct())
        {
          Log.Fatal($"Item is presented in two or more mappings, Item ID: {id}", this);
        }
      }

      stopwatch.Stop();
      Log.Info($"JsonDataProvider mappings were validated. Time spent: { stopwatch.Elapsed }", this);
    }

    private void Validate(List<ID> list, AbstractMapping mapping1, AbstractMapping mapping2)
    {
      list.AddRange(mapping1.ItemsCache.Keys.Where(x => mapping2.ItemsCache.ContainsKey(x)));
      list.AddRange(mapping2.ItemsCache.Keys.Where(x => mapping1.ItemsCache.ContainsKey(x)));
    }
  }
}