
using System;

namespace HC2.Mobs;

[Icon( "accessible_forward" )]
public sealed class Wheel : Component
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

		var rotateAxis = CharacterController.Transform.Rotation.Left;
		var rotateSpeed = Vector3.Dot( CharacterController.Transform.Rotation.Forward, CharacterController.Velocity ) * 180f / MathF.PI / Radius;

		Transform.Rotation = Rotation.FromAxis( rotateAxis, rotateSpeed * Time.Delta ) * Transform.Rotation;
	}

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected ) return;

		Gizmo.Transform = Gizmo.Transform.WithScale( 1f )
			.WithRotation( Rotation.FromYaw( 90f ) * Gizmo.Transform.Rotation );

		Gizmo.Draw.LineCircle( Vector3.Zero, Radius );
	}
}
