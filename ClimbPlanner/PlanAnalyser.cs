using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using ClimbPlanner.Models;

namespace ClimbPlanner
{
  internal class PlanAnalyser
  {
    private const string GearStash = "GearStash"; // Excluded as entity from report.

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
      _fileSystemWatcher.Filter = Path.GetFileName(planFilename);
      _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
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
      _fileSystemWatcher.EnableRaisingEvents = false;

      ParsePlanFile();

      _fileSystemWatcher.EnableRaisingEvents = true;
    }

    private void ParsePlanFile()
    {
      _routeEntitiesByName.Clear();
      _gearItems.Clear();

      try
      {
        var outputBuilder = new StringBuilder();

        Thread.Sleep(500);

        string fileContent = File.ReadAllText(_planFilename);
        Plan plan = Plan.Deserialise(fileContent);

        foreach (var action in plan.Actions)
        {
          ProcessPlanAction(action, outputBuilder);
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

    private void ProcessPlanAction(in PlanAction action, in StringBuilder outputBuilder)
    {
      outputBuilder.AppendLine();
      outputBuilder.AppendLine("<p>");
      outputBuilder.AppendLine($"<b>{action.Title}</b>");
      outputBuilder.AppendLine("<br />");

      if (action.GearTransfers.Any(gt => gt.Description?.Any() ?? false))
      {
        outputBuilder.AppendLine("<br />");
      }

      ProcessGearTransfers(action.GearTransfers, outputBuilder);

      AppendEntityGearItemsTable(outputBuilder);

      outputBuilder.AppendLine("</p><hr />");
    }

    private void ProcessGearTransfers(
      in IEnumerable<GearTransfer> gearTransfers,
      in StringBuilder outputBuilder)
    {
      foreach (var transfer in gearTransfers)
      {
        GearItem? item = null;

        if (!string.IsNullOrWhiteSpace(transfer.GearItem))
        {
          item = new GearItem(transfer.GearItem);
        }

        if (item.HasValue &&
            !_gearItems.Contains(item.Value))
        {
          _gearItems.Add(item.Value);
        }

        ProcessGearTransfer(item, transfer, outputBuilder);
      }
    }

    private void ProcessGearTransfer(
      in GearItem? item,
      in GearTransfer transfer,
      in StringBuilder outputBuilder)
    {
      if (!item.HasValue ||
          string.IsNullOrWhiteSpace(transfer.FromEntity) ||
          string.IsNullOrWhiteSpace(transfer.ToEntity))
      {
        return;
      }

      var debitEntity = GetOrCreateRouteEntity(transfer.FromEntity);
      var creditEntity = GetOrCreateRouteEntity(transfer.ToEntity);

      debitEntity.RemoveGearItem(item.Value, transfer.Quantity);
      creditEntity.AssignGearItem(item.Value, transfer.Quantity);

      if (!string.IsNullOrWhiteSpace(transfer.Description))
      {
        outputBuilder.AppendLine($"{transfer.FromEntity} => {transfer.ToEntity} <b>:</b> ");
        outputBuilder.AppendLine($"({transfer.GearItem} x {transfer.Quantity}) <b>:</b> {transfer.Description}");
        outputBuilder.AppendLine("<br />");
      }
    }

    private RouteEntity GetOrCreateRouteEntity(in string name)
    {
      if (!_routeEntitiesByName.ContainsKey(name))
      {
        _routeEntitiesByName.Add(name, new RouteEntity(name));
      }

      return _routeEntitiesByName[name];
    }

    private void AppendEntityGearItemsTable(in StringBuilder outputBuilder)
    {
      outputBuilder.Append("<p><table border='1'>");
      outputBuilder.Append("<tr><td></td>");

      foreach (var entity in _routeEntitiesByName.Values)
      {
        if (entity.Name.Equals(GearStash, StringComparison.OrdinalIgnoreCase))
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
          if (entity.Name.Equals(GearStash, StringComparison.OrdinalIgnoreCase))
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
