using System;

/// <summary>
/// I've pulled this out into its own component because I feel like we'll want different camera behaviors when we have weapons, building, etc..
/// </summary>
public sealed class CameraController : Component
{
	/// <summary>
	/// The player
	/// </summary>
	[RequireComponent]
	public Player Player { get; set; }

	/// <summary>
	/// The camera
	/// </summary>
	[Property, Group( "Setup" )] 
	public CameraComponent Camera { get; set; }

	/// <summary>
	/// The boom arm for the character's camera.
	/// </summary>
	[Property, Group( "Setup" )] 
	public GameObject Boom { get; set; }

	/// <summary>
	/// How far can we look up / down?
	/// </summary>
	[Property, Group( "Config" )]
	public Vector2 PitchLimits { get; set; } = new( -50f, 50f );

	[Property, Group( "Config" )]
	public float VelocityFOVScale { get; set; } = 50f;

	bool IsAiming => Input.Down( "attack2" ) && Player.MainHandWeapon.IsValid() && Player.MainHandWeapon.AimingEnabled;

	/// <summary>
	/// Constructs a ray using the camera's GameObject
	/// </summary>
	public Ray AimRay
	{
		get
		{
			return new( Transform.Position + Vector3.Up * 64f, Player.EyeAngles.ToRotation().Forward );
		}
	}

	protected override void OnStart()
	{
		// Turn off the camera if we're not in charge of this player
		Camera.Enabled = !Player.IsProxy;
	}

	/// <summary>
	/// Runs from <see cref="Player.OnUpdate"/>
	/// </summary>
	public void UpdateFromPlayer()
	{
		// Have an option for this later to scale?
		Player.EyeAngles += IsAiming ? Input.AnalogLook * 0.45f : Input.AnalogLook;
		Player.EyeAngles = Player.EyeAngles.WithPitch( Player.EyeAngles.pitch.Clamp( PitchLimits.x, PitchLimits.y ) );

		Boom.Transform.Rotation = Player.EyeAngles.ToRotation();

		Boom.Transform.Position = Boom.Transform.Position.LerpTo( Player.Character.Transform.Position + Vector3.Up * Player.GetDuckHeight() * 0.95f, Time.Delta * 10 );

		var targetFov = Preferences.FieldOfView + Player.Character.Velocity.Length / VelocityFOVScale;

		if ( IsAiming )
		{
			targetFov -= Player.MainHandWeapon.AimingFOVOffset;
		}

		Camera.FieldOfView = Camera.FieldOfView.LerpTo( targetFov, Time.Delta * 10 );

	}

	public float CalcRelativeYaw( float angle )
	{
		float length = Camera.Transform.Rotation.Yaw() - angle;

		float d = MathX.UnsignedMod( Math.Abs( length ), 360 );
		float r = (d > 180) ? 360 - d : d;
		r *= (length >= 0 && length <= 180) || (length <= -180 && length >= -360) ? 1 : -1;

		return r;
	}
}
