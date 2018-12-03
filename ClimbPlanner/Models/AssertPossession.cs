namespace ClimbPlanner.Models
{
  internal class AssertPossession
  {
    public string Entity { get; }
    public string GearItem { get; }
    public int Quantity { get; }

    public AssertPossession(
      in string entity,
      in string gearItem,
      in int quantity)
    {
      Entity = entity;
      GearItem = gearItem;
      Quantity = quantity;
    }
  }
}
