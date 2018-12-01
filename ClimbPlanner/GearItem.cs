namespace ClimbPlanner
{
  internal struct GearItem
  {
    public string Name { get; }

    public GearItem(in string name)
    {
      Name = name;
    }
  }
}
