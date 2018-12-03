using System.Collections.Generic;

using ClimbPlanner.Models;

namespace ClimbPlanner
{
  internal class RouteEntity
  {
    public string Name { get; }
    public IReadOnlyDictionary<GearItem, int> QuantityByGearItem => _quantityByGearItem;

    private readonly Dictionary<GearItem, int> _quantityByGearItem = new Dictionary<GearItem, int>();

    public RouteEntity(in string name)
    {
      Name = name;
    }

    public void AssignGearItem(in GearItem item, in int quantity)
    {
      if (!_quantityByGearItem.ContainsKey(item))
      {
        _quantityByGearItem.Add(item, 0);
      }

      _quantityByGearItem[item] += quantity;
    }

    public void RemoveGearItem(in GearItem item, in int quantity)
    {
      if (!_quantityByGearItem.ContainsKey(item))
      {
        _quantityByGearItem.Add(item, 0);
      }

      _quantityByGearItem[item] -= quantity;
    }
  }
}
