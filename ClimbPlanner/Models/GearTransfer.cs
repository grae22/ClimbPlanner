namespace ClimbPlanner.Models
{
  internal struct GearTransfer
  {
    public string FromEntity { get; }
    public string ToEntity { get; }
    public GearItem GearItem { get; }
    public int Quantity { get; }

    public GearTransfer(
      string fromEntity,
      string toEntity,
      GearItem gearItem,
      int quantity)
    {
      FromEntity = fromEntity;
      ToEntity = toEntity;
      GearItem = gearItem;
      Quantity = quantity;
    }
  }
}
