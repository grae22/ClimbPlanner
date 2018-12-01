using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ClimbPlanner
{
  internal class PlanAnalyser
  {
    private enum PlanFileFields
    {
      RouteEntity,
      Description,
      GearItem,
      Quantity,
      DebitEntity,
      CreditEntity,
      Count
    }

    private readonly FileSystemWatcher _fileSystemWatcher;
    private readonly string _planFilename;
    private readonly Dictionary<string, RouteEntity> _routeEntitiesByName = new Dictionary<string, RouteEntity>();
    private readonly List<GearItem> _gearItems = new List<GearItem>();

    public PlanAnalyser(in string planFilename)
    {
      _planFilename = planFilename;

      string directoryPath = Path.GetDirectoryName(planFilename);

      _fileSystemWatcher = new FileSystemWatcher(directoryPath);
      _fileSystemWatcher.EnableRaisingEvents = true;
      _fileSystemWatcher.Changed += OnPlanFileChanged;

      OnPlanFileChanged(
        null,
        new FileSystemEventArgs(
          WatcherChangeTypes.Changed,
          directoryPath,
          Path.GetFileName(_planFilename)));
    }

    private void OnPlanFileChanged(object source, FileSystemEventArgs args)
    {
      if (!args.FullPath.Equals(_planFilename, StringComparison.OrdinalIgnoreCase))
      {
        return;
      }

      ParsePlanFile();
    }

    private void ParsePlanFile()
    {
      _routeEntitiesByName.Clear();
      _gearItems.Clear();

      try
      {
        var outputBuilder = new StringBuilder();

        Thread.Sleep(500);

        string[] fileLines = File.ReadAllLines(_planFilename);

        foreach (string line in fileLines)
        {
          ProcessPlanFileLine(line, outputBuilder);
        }

        File.WriteAllText(
          $"{_planFilename}.output.html",
          outputBuilder.ToString());

        Console.WriteLine("Parsed OK");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to parse: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
      }
    }

    private void ProcessPlanFileLine(in string line, in StringBuilder outputBuilder)
    {
      string[] fields = line.Split('|');

      if (fields.Length != (int)PlanFileFields.Count)
      {
        throw new Exception($"Incorrect number of fields: \"{line}\"");
      }

      var entityName = fields[(int)PlanFileFields.RouteEntity].Trim();
      var description = fields[(int)PlanFileFields.Description].Trim();
      var itemName = fields[(int)PlanFileFields.GearItem].Trim();
      var debitEntityName = fields[(int)PlanFileFields.DebitEntity].Trim();
      var creditEntityName = fields[(int)PlanFileFields.CreditEntity].Trim();
      var quantity = int.Parse(fields[(int)PlanFileFields.Quantity]);

      GearItem? item = null;

      if (!string.IsNullOrWhiteSpace(itemName))
      {
        item = new GearItem(itemName);
      }

      GetOrCreateRouteEntity(entityName);

      if (item.HasValue &&
          !_gearItems.Contains(item.Value))
      {
        _gearItems.Add(item.Value);
      }

      if (item.HasValue &&
          !string.IsNullOrWhiteSpace(debitEntityName) &&
          !string.IsNullOrWhiteSpace(creditEntityName))
      {
        var debitEntity = GetOrCreateRouteEntity(debitEntityName);
        var creditEntity = GetOrCreateRouteEntity(creditEntityName);

        debitEntity.RemoveGearItem(item.Value, quantity);
        creditEntity.AssignGearItem(item.Value, quantity);
      }

      outputBuilder.AppendLine();
      outputBuilder.AppendLine($"{entityName} : {description}");

      AppendEntityGearItems(outputBuilder);
    }

    private RouteEntity GetOrCreateRouteEntity(in string name)
    {
      if (!_routeEntitiesByName.ContainsKey(name))
      {
        _routeEntitiesByName.Add(name, new RouteEntity(name));
      }

      return _routeEntitiesByName[name];
    }

    private void AppendEntityGearItems(in StringBuilder outputBuilder)
    {
      outputBuilder.Append("<p><table border='1'>");
      outputBuilder.Append("<tr><td></td>");

      foreach (var entity in _routeEntitiesByName.Values)
      {
        if (entity.Name.Equals("GearStash", StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        outputBuilder.AppendLine($"<td>{entity.Name}</td>");
      }

      outputBuilder.Append("<td>Total</td></tr>");

      foreach(var item in _gearItems)
      {
        int total = 0;

        outputBuilder.Append($"<tr><td>{item.Name}</td>");

        foreach (var entity in _routeEntitiesByName.Values)
        {
          if (entity.Name.Equals("GearStash", StringComparison.OrdinalIgnoreCase))
          {
            continue;
          }

          outputBuilder.Append("<td align='center'>");

          if (entity.QuantityByGearItem.ContainsKey(item))
          {
            outputBuilder.Append($"{entity.QuantityByGearItem[item]}");
            
            total += entity.QuantityByGearItem[item];
          }

          outputBuilder.Append("</td>");
        }

        outputBuilder.Append($"<td align='center'>{total}</td></tr>");
      }

      outputBuilder.Append("</table></p>");
    }
  }
}
