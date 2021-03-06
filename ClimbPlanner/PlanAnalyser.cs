﻿using System;
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

    private bool _errorsDetected;

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

      _errorsDetected = false;

      try
      {
        var outputBuilder = new StringBuilder();
        outputBuilder.Append("<html><body>");

        Thread.Sleep(500);

        string fileContent = File.ReadAllText(_planFilename);
        Plan plan = Plan.Deserialise(fileContent);

        foreach (var action in plan.Actions)
        {
          ProcessPlanAction(action, outputBuilder);
        }

        outputBuilder.Append("</body></html>");

        string output = outputBuilder.ToString();

        ApplyBodyStyling(ref output, _errorsDetected);

        File.WriteAllText(
          $"{_planFilename}.output.html",
          output);

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
      outputBuilder.AppendLine($"<font size='+1'><b>{action.Title}</b></font>");
      outputBuilder.AppendLine("<br />");

      outputBuilder.AppendLine("<p>");

      ProcessLocationChanges(action.LocationChanges);
      ProcessGearTransfers(action.GearTransfers, outputBuilder);

      outputBuilder.AppendLine("</p>");

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

      if (!AreEntitiesAtSameLocation(debitEntity, creditEntity))
      {
        outputBuilder.AppendLine(
          $"<div style='color:red;'><b>ERROR: {transfer.FromEntity} => {transfer.ToEntity} [{transfer.GearItem} x {transfer.Quantity}] Locations do not match!</b></div>");

        _errorsDetected = true;

        return;
      }

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

      if (debitEntity.QuantityByGearItem[item.Value] < 0 &&
          !debitEntity.Name.Equals(GearStash, StringComparison.OrdinalIgnoreCase))
      {
        _errorsDetected = true;
      }

      if (!string.IsNullOrWhiteSpace(transfer.Description))
      {
        outputBuilder.AppendLine($"{transfer.FromEntity} => {transfer.ToEntity} <b>:</b> ");
        outputBuilder.AppendLine($"[{transfer.GearItem} x {transfer.Quantity}] <b>:</b> {transfer.Description}");
        outputBuilder.AppendLine("<br />");
      }
    }

    private void ProcessLocationChanges(in IEnumerable<LocationChange> locationChanges)
    {
      if (locationChanges == null)
      {
        return;
      }

      foreach (var change in locationChanges)
      {
        RouteEntity entity = GetOrCreateRouteEntity(change.Entity);

        entity.ChangeLocation(change.NewLocation);
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
          $"<div style='color:red;'><b>ERROR: \"{entity.Name}\" requires {assert.Quantity} \"{assert.GearItem}(s)\"</b></div>");

        _errorsDetected = true;
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
      outputBuilder.Append("<p><table border='1' style='border-collapse:collapse; border-color:#b0b0b0; background-color:white;' cellpadding='3px'>");
      outputBuilder.Append("<tr><td bgcolor='#f0f0f0'></td>");

      // Column headers (entities).
      foreach (var entity in _routeEntitiesByName.Values)
      {
        if (entity.Name.Equals(GearStash, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        string location = entity.Location == entity.Name ? string.Empty : entity.Location;

        outputBuilder.AppendLine(
          $"<td bgcolor='#f0f0ff' valign='top'><b>{entity.Name}</b><br /><font size='-1'>{location}</font></td>");
      }

      outputBuilder.Append("<td bgcolor='#fff0f0'><b>Total<b/></td></tr>");

      // Rows (items).
      var sortedGearItems = new SortedList<string, GearItem>();

      _gearItems.ForEach(g => sortedGearItems.Add(g.Name, g));

      foreach(var item in sortedGearItems.Values)
      {
        int total = 0;

        outputBuilder.Append($"<tr><td bgcolor='#f0fff0'><b>{item.Name}</b></td>");

        foreach (var entity in _routeEntitiesByName.Values)
        {
          if (entity.Name.Equals(GearStash, StringComparison.OrdinalIgnoreCase))
          {
            continue;
          }

          if (entity.QuantityByGearItem.ContainsKey(item))
          {
            int quantity = entity.QuantityByGearItem[item];

            int quantityChange = _itemQuantityChangeTracker.GetChangeInQuantitySinceLastCheck(entity.Name, item.Name);

            // Quantity changes are emphasized.
            string colour;

            if (quantityChange > 0)
            {
              colour = "#40cf40";
            }
            else if (quantityChange < 0)
            {
              colour = "#ffa030";
            }
            else
            {
              colour = "#ffffff";
            }

            // Negative quantities are emphasized;
            if (quantity < 0)
            {
              colour = "#ff0000";
            }

            string changeHtml = string.Empty;

            if (quantityChange != 0)
            {
              changeHtml = $"(<font size='-1'>{quantityChange:+#;-#}</font>)";
            }

            if (quantity > 0 || quantityChange != 0)
            {
              outputBuilder.Append($"<td align='center' bgcolor='{colour}'>{quantity} {changeHtml}</td>");
            }
            else
            {
              outputBuilder.Append("<td></td>");
            }

            total += entity.QuantityByGearItem[item];
          }
          else
          {
            outputBuilder.Append("<td></td>");
          }
        }

        outputBuilder.Append($"<td align='center' bgcolor='#fff0f0'><b>{total}</b></td></tr>");
      }

      outputBuilder.Append("</table></p>");
    }

    private static void ApplyBodyStyling(
      ref string html,
      in bool errorsDetected)
    {
      string backgroundColour = errorsDetected ? "#ffdfdf" : "#ffffff";

      html = html.Replace(
        "<body>",
        $"<body style='font-family:consolas; background-color:{backgroundColour};'>");
    }

    private static bool AreEntitiesAtSameLocation(in RouteEntity entity1, in RouteEntity entity2)
    {
      return
        entity1.Location.Equals(GearStash, StringComparison.OrdinalIgnoreCase) ||
        entity2.Location.Equals(GearStash, StringComparison.OrdinalIgnoreCase) ||
        entity1.Location.Equals(entity2.Location, StringComparison.OrdinalIgnoreCase);
    }
  }
}
