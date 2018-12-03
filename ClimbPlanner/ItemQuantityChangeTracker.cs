using System.Collections.Generic;

namespace ClimbPlanner
{
  internal class ItemQuantityChangeTracker
  {
    private readonly Dictionary<string, int> _quantityChangeByItemEntityHash = new Dictionary<string, int>();

    public void Add(
      in string entityName,
      in string itemName,
      in int quantity)
    {
      string key = GetKey(entityName, itemName);

      if (!_quantityChangeByItemEntityHash.ContainsKey(key))
      {
        _quantityChangeByItemEntityHash.Add(key, 0);
      }

      _quantityChangeByItemEntityHash[key] += quantity;
    }

    public void Subtract(
      in string entityName,
      in string itemName,
      in int quantity)
    {
      string key = GetKey(entityName, itemName);

      if (!_quantityChangeByItemEntityHash.ContainsKey(key))
      {
        _quantityChangeByItemEntityHash.Add(key, 0);
      }

      _quantityChangeByItemEntityHash[key] -= quantity;
    }

    public int GetChangeInQuantitySinceLastCheck(
      in string entityName,
      in string itemName)
    {
      string key = GetKey(entityName, itemName);

      if (!_quantityChangeByItemEntityHash.ContainsKey(key))
      {
        _quantityChangeByItemEntityHash.Add(key, 0);
      }

      int tmp = _quantityChangeByItemEntityHash[key];

      _quantityChangeByItemEntityHash[key] = 0;

      return tmp;
    }

    public void Reset()
    {
      _quantityChangeByItemEntityHash.Clear();
    }

    private static string GetKey(in string entityName, in string itemName)
    {
      return $"{entityName}+{itemName}";
    }
  }
}
