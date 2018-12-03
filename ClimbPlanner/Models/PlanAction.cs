using System.Collections.Generic;

namespace ClimbPlanner.Models
{
  internal struct PlanAction
  {
    public string Title { get; }
    public IEnumerable<GearTransfer> GearTransfers { get; }

    public PlanAction(
      string title,
      IEnumerable<GearTransfer> gearTransfers)
    {
      Title = title;
      GearTransfers = gearTransfers;
    }
  }
}
