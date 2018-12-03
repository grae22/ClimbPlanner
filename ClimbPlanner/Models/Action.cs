using System.Collections.Generic;

namespace ClimbPlanner.Models
{
  internal struct Action
  {
    public string Title { get; }
    public IEnumerable<GearTransfer> GearTransfers { get; }

    public Action(
      string title,
      IEnumerable<GearTransfer> gearTransfers)
    {
      Title = title;
      GearTransfers = gearTransfers;
    }
  }
}
