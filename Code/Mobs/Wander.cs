
namespace HC2.Mobs;

#nullable enable

[Icon( "hiking" )]
public sealed class Wander : Component
{
	[RequireComponent] public Mob Mob { get; private set; } = null!;

	[Property] public float MaxRangeFromSpawn { get; set; } = 1024f;

	[Property] public Curve Distance { get; set; } = new Curve( new Curve.Frame( 0f, 256f ), new Curve.Frame( 1f, 512f ) );
	[Property] public Curve PauseTime { get; set; } = new Curve( new Curve.Frame( 0f, 1f ), new Curve.Frame( 1f, 4f ) );

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
	}
}

