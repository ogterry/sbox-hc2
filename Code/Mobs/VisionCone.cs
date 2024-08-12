using System;
using System.Diagnostics.CodeAnalysis;

namespace HC2.Mobs;

#nullable enable

[Icon( "visibility" )]
public sealed class VisionCone : Component
{
	[Property, Range( 0f, 175f )]
	public float Angle { get; set; } = 90f;

	[Property, Range( 0f, 2048f )]
	public float MaxRange { get; set; } = 512f;

	[Pure]
	public bool CanSeeAnyTarget( [NotNullWhen( true )] out MobTarget? target )
	{
		// TODO: helper for getting objects within a certain range?

		var maxRangeSq = MaxRange * MaxRange;

		var targets = Scene.GetAllComponents<MobTarget>()
			.Select( x => (Target: x, Diff: x.Transform.Position - Transform.Position) )
			.Where( x => x.Diff.LengthSquared >= 16f && x.Diff.LengthSquared <= maxRangeSq )
			.OrderBy( x => x.Diff.LengthSquared );

		var forward = Transform.Rotation.Forward;
		var minDot = MathF.Cos( Angle * MathF.PI / 360f );

		foreach ( var (possibleTarget, diff) in targets )
		{
			var dot = Vector3.Dot( forward, diff.Normal );

			if ( dot < minDot ) continue;

			target = possibleTarget;
			return true;
		}

		target = null;
		return false;
	}

	protected override void DrawGizmos()
	{
		if ( !Gizmo.HasSelected ) return;

		var radius = MathF.Tan( Angle * MathF.PI / 360f ) * MaxRange;

		Gizmo.Draw.Color = Color.White.WithAlpha( 0.25f );
		Gizmo.Draw.SolidCone( Vector3.Forward * MaxRange, -Vector3.Forward * MaxRange, radius );
	}
}
