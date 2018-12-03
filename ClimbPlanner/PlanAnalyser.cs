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
    private readonly ItemQuantityChangeTracker _itemQuantityChangeTracker = new ItemQuantityChangeTracker();

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

      try
      {
        ParsePlanFile();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
      }

      _fileSystemWatcher.EnableRaisingEvents = true;
    }

    private void ParsePlanFile()
    {
      _routeEntitiesByName.Clear();
      _gearItems.Clear();
      _itemQuantityChangeTracker.Reset();

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

      if (action.GearTransfers.Any(gt => gt.Quantity > 0))
      {
        AppendEntityGearItemsTable(outputBuilder);
      }

      ProcessPossessionAsserts(action.PossessionAsserts, outputBuilder);

      outputBuilder.AppendLine("</p><hr />");
    }

    private void ProcessGearTransfers(
      in IEnumerable<GearTransfer> gearTransfers,
      in StringBuilder outputBuilder)
    {
      if (gearTransfers == null)
      {
        return;
      }

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

      if (transfer.Quantity < 0)
      {
        throw new ArgumentException("Transfer quantity cannot be negative.", nameof(transfer));
      }

      var debitEntity = GetOrCreateRouteEntity(transfer.FromEntity);
      var creditEntity = GetOrCreateRouteEntity(transfer.ToEntity);

      debitEntity.RemoveGearItem(item.Value, transfer.Quantity);
      creditEntity.AssignGearItem(item.Value, transfer.Quantity);

      _itemQuantityChangeTracker.Add(
        transfer.ToEntity,
        item.Value.Name,
        transfer.Quantity);

      _itemQuantityChangeTracker.Subtract(
        transfer.FromEntity,
        item.Value.Name,
        transfer.Quantity);

      if (!string.IsNullOrWhiteSpace(transfer.Description))
      {
        outputBuilder.AppendLine($"{transfer.FromEntity} => {transfer.ToEntity} <b>:</b> ");
        outputBuilder.AppendLine($"({transfer.GearItem} x {transfer.Quantity}) <b>:</b> {transfer.Description}");
        outputBuilder.AppendLine("<br />");
      }
    }

    private void ProcessPossessionAsserts(
      in IEnumerable<AssertPossession> possessionAsserts,
      in StringBuilder outputBuilder)
    {
      if (possessionAsserts == null)
      {
        return;
      }

      foreach (var assert in possessionAsserts)
      {
        RouteEntity entity = GetOrCreateRouteEntity(assert.Entity);

        var item = new GearItem(assert.GearItem);

        if (entity.QuantityByGearItem.ContainsKey(item) &&
            entity.QuantityByGearItem[item] >= assert.Quantity)
        {
          continue;
        }

        outputBuilder.AppendLine(
          $"<div style='color:red;'>\"{entity.Name}\" requires {assert.Quantity} \"{assert.GearItem}(s)\"</div>");
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
      outputBuilder.Append("<p><table border='1' style='border-collapse:collapse; border-color:#b0b0b0;' cellpadding='3px'>");
      outputBuilder.Append("<tr><td></td>");

      // Column headers (entities).
      foreach (var entity in _routeEntitiesByName.Values)
      {
        if (entity.Name.Equals(GearStash, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        outputBuilder.AppendLine($"<td bgcolor='#f0f0ff'><b>{entity.Name}</b></td>");
      }

      outputBuilder.Append("<td bgcolor='#fff0f0'><b>Total<b/></td></tr>");

      // Rows (items).
      foreach(var item in _gearItems)
      {
        int total = 0;

        outputBuilder.Append($"<tr><td bgcolor='#f0fff0'><b>{item.Name}</b></td>");

        foreach (var entity in _routeEntitiesByName.Values)
        {
          if (entity.Name.Equals(GearStash, StringComparison.OrdinalIgnoreCase))
          {
            continue;
          }

          outputBuilder.Append("<td align='center'>");

          if (entity.QuantityByGearItem.ContainsKey(item))
          {
            int quantity = entity.QuantityByGearItem[item];

            int quantityChange = _itemQuantityChangeTracker.GetChangeInQuantitySinceLastCheck(entity.Name, item.Name);

            // Quantity changes are emphasized.
            string colour;

            if (quantityChange > 0)
            {
              colour = "#00c000";
            }
            else if (quantityChange < 0)
            {
              colour = "#f00000";
            }
            else
            {
              colour = "black";
            }

            // Negative quantities are emphasized;
            if (quantity > -1)
            {
              outputBuilder.Append($"<span style='color:{colour};'>{quantity}</span>");
            }
            else
            {
              outputBuilder.Append($"<span style='color:{colour}; font-weight:bold;'><b>({quantity})</b></span>");
            }

            total += entity.QuantityByGearItem[item];
          }

          outputBuilder.Append("</td>");
        }

        outputBuilder.Append($"<td align='center' bgcolor='#fff0f0'>{total}</td></tr>");
      }

      outputBuilder.Append("</table></p>");
    }
  }
}
