using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HC2;

public enum GatherSourceKind
{
	None,
	Wood,
	Stone,
	Metal
}

public sealed class ResourceGatherer : WeaponComponent
{
	[Property] public Dictionary<GatherSourceKind, float> Effectiveness { get; set; } = new();
}
