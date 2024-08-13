using System;

namespace HC2.Mobs;

#nullable enable

[Icon( "sports_soccer" )]
public sealed class Rollerball : Component
{
	[Property]
	public CharacterController? CharacterController { get; private set; }

	[Property, Range( 1f, 256f )]
	public float Radius { get; set; } = 16f;

	protected override void OnFixedUpdate()
	{
		if ( CharacterController is null )
		{
			return;
		}

		var groundVelocity = CharacterController.Velocity.WithZ( 0f );

		if ( groundVelocity.IsNearZeroLength )
		{
			return;
		}

		var rotateAxis = Vector3.Cross( Vector3.Up, groundVelocity ).Normal;
		var rotateSpeed = groundVelocity.Length * 180f / MathF.PI / Radius;

		Transform.Rotation = Rotation.FromAxis( rotateAxis, rotateSpeed * Time.Delta ) * Transform.Rotation;
	}

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected ) return;

		Gizmo.Transform = Gizmo.Transform.WithScale( 1f );

		Gizmo.Draw.LineSphere( Vector3.Zero, Radius );
	}
}
