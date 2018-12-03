using System.Collections.Generic;

namespace ClimbPlanner.Models
{
  internal struct PlanAction
  {
    public string Title { get; }
    public IEnumerable<GearTransfer> GearTransfers { get; }
    public IEnumerable<AssertPossession> PossessionAsserts { get; }

    public PlanAction(
      string title,
      IEnumerable<GearTransfer> gearTransfers,
      IEnumerable<AssertPossession> possessionAsserts)
    {
      Title = title;
      GearTransfers = gearTransfers;
      PossessionAsserts = possessionAsserts;
    }
  }
}
