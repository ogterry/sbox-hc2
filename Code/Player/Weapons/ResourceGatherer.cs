
namespace HC2;

[Icon( "shopping_cart" )]
public sealed class ResourceGatherer : Component
{
	[Property] public GatherSourceKind SourceKind { get; set; }

	[Range( 0f, 4f )] [Property] public float Effectiveness { get; set; } = 1f;
}
