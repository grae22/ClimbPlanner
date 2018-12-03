namespace ClimbPlanner.Models
{
  internal struct GearTransfer
  {
    public string FromEntity { get; }
    public string ToEntity { get; }
    public string GearItem { get; }
    public int Quantity { get; }

    public GearTransfer(
      string fromEntity,
      string toEntity,
      string gearItem,
      int quantity)
    {
      FromEntity = fromEntity;
      ToEntity = toEntity;
      GearItem = gearItem;
      Quantity = quantity;
    }
  }
}
