using System.Collections.Generic;

using Newtonsoft.Json;

namespace ClimbPlanner.Models
{
  internal struct Plan
  {
    public IEnumerable<PlanAction> Actions { get; }

    public Plan(IEnumerable<PlanAction> actions)
    {
      Actions = actions;
    }

    public string Serialise()
    {
      return JsonConvert.SerializeObject(this);
    }

    public static Plan Deserialise(in string serialisedPlan)
    {
      return JsonConvert.DeserializeObject<Plan>(serialisedPlan);
    }
  }
}
