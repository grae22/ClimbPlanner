using System.Collections.Generic;

using Newtonsoft.Json;

namespace ClimbPlanner.Models
{
  internal struct Plan
  {
    public IEnumerable<Action> Actions { get; }

    public Plan(IEnumerable<Action> actions)
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
