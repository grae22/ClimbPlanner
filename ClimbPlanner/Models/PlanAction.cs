using System.Collections.Generic;

namespace ClimbPlanner.Models
{
  internal struct PlanAction
  {
    public string Title { get; }
    public IEnumerable<GearTransfer> GearTransfers { get; }
    public IEnumerable<LocationChange> LocationChanges { get; }
    public IEnumerable<AssertPossession> PossessionAsserts { get; }

    public PlanAction(
      string title,
      IEnumerable<GearTransfer> gearTransfers,
      IEnumerable<LocationChange> locationChanges,
      IEnumerable<AssertPossession> possessionAsserts)
    {
      Title = title;
      GearTransfers = gearTransfers;
      LocationChanges = locationChanges;
      PossessionAsserts = possessionAsserts;
    }
  }
}
