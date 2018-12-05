namespace ClimbPlanner.Models
{
  internal class LocationChange
  {
    public string Entity { get; }
    public string NewLocation { get; }

    public LocationChange(
      string entity,
      string newLocation)
    {
      Entity = entity;
      NewLocation = newLocation;
    }
  }
}
