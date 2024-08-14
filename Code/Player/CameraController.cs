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
	public GameObject CameraTarget { get; set; }

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

	/// <summary>
	/// Bobbing cycle time
	/// </summary>
	[Property, Group( "Config" )]
	public float BobCycleTime { get; set; } = 5.0f;

	/// <summary>
	/// Bobbing direction
	/// </summary>
	[Property, Group( "Config" )]
	public Vector3 BobDirection { get; set; } = new Vector3( 0.0f, 1.0f, 0.5f );

	private float bobAnim;
	private float bobSpeed;

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
		if ( !(Scene?.Camera?.IsValid() ?? false) )
		{
			var prefab = ResourceLibrary.Get<PrefabFile>( "prefabs/camera.prefab" );
			SceneUtility.GetPrefabScene( prefab ).Clone( position: CameraTarget.Transform.Position, rotation: CameraTarget.Transform.Rotation );
		}
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

		Boom.Transform.Position = Boom.Transform.Position.LerpTo( Player.Character.Transform.Position + Vector3.Up * Player.GetDuckHeight() * 0.8f, Time.Delta * 10 );


		if ( !Player.Character.IsOnGround )
		{
			bobSpeed = bobSpeed.LerpTo( 0, Time.Delta * 10 );
		}
		else
		{
			bobSpeed = bobSpeed.LerpTo( Player.Character.Velocity.WithZ( 0 ).Length, Time.Delta * 10 );
		}

		Boom.Transform.Position += CalcBobbingOffset( Player.Character.Velocity.Length ) * Boom.Transform.Rotation * Time.Delta * 100f;

		var targetFov = Preferences.FieldOfView + Player.Character.Velocity.Length / VelocityFOVScale;

		if ( IsAiming )
		{
			targetFov -= Player.MainHandWeapon.AimingFOVOffset;
		}

		Scene.Camera.FieldOfView = Scene.Camera.FieldOfView.LerpTo( targetFov, Time.Delta * 10 );
		Scene.Camera.Transform.Position = CameraTarget.Transform.Position.WithZ( Scene.Camera.Transform.Position.z );
		Scene.Camera.Transform.Position = Scene.Camera.Transform.Position.LerpTo( CameraTarget.Transform.Position, Time.Delta * 15 );
		Scene.Camera.Transform.Rotation = CameraTarget.Transform.Rotation;
	}

	public float CalcRelativeYaw( float angle )
	{
		float length = CameraTarget.Transform.Rotation.Yaw() - angle;

		float d = MathX.UnsignedMod( Math.Abs( length ), 360 );
		float r = (d > 180) ? 360 - d : d;
		r *= (length >= 0 && length <= 180) || (length <= -180 && length >= -360) ? 1 : -1;

		return r;
	}

	private Vector3 CalcBobbingOffset( float speed )
	{
		bobAnim += Time.Delta * BobCycleTime;

		var twoPI = System.MathF.PI * 2.0f;

		if ( bobAnim > twoPI )
		{
			bobAnim -= twoPI;
		}

		var offset = BobDirection * (speed * 0.005f) * System.MathF.Cos( bobAnim );
		offset = offset.WithZ( -System.MathF.Abs( offset.z ) );

		return offset;
	}
}
