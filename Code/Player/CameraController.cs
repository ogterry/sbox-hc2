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
	public Vector2 PitchLimits { get; set; } = new( -50, 50 );

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
		Player.EyeAngles += Input.AnalogLook;
		Player.EyeAngles = Player.EyeAngles.WithPitch( Player.EyeAngles.pitch.Clamp( PitchLimits.x, PitchLimits.y ) );

		Boom.Transform.Rotation = Player.EyeAngles.ToRotation();

		//change fov based on player's movement speed
		Camera.FieldOfView = Camera.FieldOfView.LerpTo( 90 + Player.Character.Velocity.Length / 50, Time.Delta * 10 );
	}
}
