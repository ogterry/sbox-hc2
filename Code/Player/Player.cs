using HC2;
using Sandbox.Citizen;
using Sandbox.Events;

public sealed class Player : Component, IDamage, IGameEventHandler<KilledEvent>
{
	/// <summary>
	/// The player's character controller, handles movement
	/// </summary>
	[RequireComponent]
	public CharacterController Character { get; set; }

	[RequireComponent]
	public HealthComponent HealthComponent { get; set; }

	/// <summary>
	/// Lil' helper for the citizen animations
	/// </summary>
	[Property, Group( "Components" )]
	public CitizenAnimationHelper AnimationHelper { get; set; }

	/// <summary>
	/// Our skinned model renderer
	/// </summary>
	[Property, Group( "Components" )]
	public SkinnedModelRenderer ModelRenderer { get; set; }

	/// <summary>
	/// The camera controller which controls the camera
	/// </summary>
	[Property, Group( "Components" )]
	public CameraController CameraController { get; set; }

	/// <summary>
	/// Base jump power, can possibly change
	/// </summary>
	[Property, Group( "Movement Config" )]
	public float JumpPower { get; set; } = 1024f;

	/// <summary>
	/// Base movement speed, can possibly change
	/// </summary>
	[Property, Group( "Movement Config" )]
	public float MovementSpeed { get; set; } = 256f;

	/// <summary>
	/// The base acceleration in the air
	/// </summary>
	[Property, Group( "Movement Config" )]
	private float AirAcceleration { get; set; } = 5f;

	/// <summary>
	/// The base acceleration on foot
	/// </summary>
	[Property, Group( "Movement Config" )]
	private float Acceleration { get; set; } = 10f;

	/// <summary>
	/// The fastest acceleration you can move while in the air
	/// </summary>
	[Property, Group( "Movement Config" )]
	private float MaxAirAcceleration { get; set; } = 125f;

	/// <summary>
	/// The fastest acceleration you can move on foot
	/// </summary>
	[Property, Group( "Movement Config" )]
	private float MaxAcceleration { get; set; } = 500f;

	/// <summary>
	/// Which way are we looking?
	/// </summary>
	[Sync] 
	public Angles EyeAngles { get; set; }

	/// <summary>
	/// What's our target speed?
	/// </summary>
	[Sync] 
	private Vector3 WishVelocity { get; set; }
	
	/// <summary>
	/// How much do we wish to move by? (Normal)
	/// </summary>
	[Sync]
	private Vector3 WishMove { get; set; }

	/// <summary>
	/// How sticky are we to the ground?
	/// </summary>
	private float Friction { get; set; } = 10;

	/// <summary>
	/// What's our holdtype?
	/// </summary>
	public CitizenAnimationHelper.HoldTypes HoldType { get; set; }

	/// <summary>
	/// What's our friction
	/// </summary>
	/// <returns></returns>
	private float GetFriction()
	{
		if ( Character.IsOnGround ) 
			return Friction;

		// Base air friction, not gonna bother having it customizable
		return 0.2f;
	}

	/// <summary>
	/// How fast can we move?
	/// I've made this a method as I assume we'll want to adjust this with levels, skills, armors, etc.
	/// </summary>
	/// <returns></returns>
	private Vector3 GetWishSpeed()
	{
		return MovementSpeed;
	}

	/// <summary>
	/// What's our jump strength?
	/// I've made this a method as I assume we'll want to adjust this with levels, skills, armors, etc.
	/// </summary>
	/// <returns></returns>
	private float GetJumpPower()
	{
		return JumpPower;
	}

	/// <summary>
	/// Build how quick we wanna move
	/// </summary>
	private void BuildWishVelocity()
	{
		WishVelocity = 0f;
		WishMove = Input.AnalogMove;

		var rot = EyeAngles.WithPitch( 0f ).ToRotation();

		var wishDirection = WishMove.Normal * rot;
		wishDirection = wishDirection.WithZ( 0 );
		WishVelocity = wishDirection * GetWishSpeed();
		WishVelocity = WishVelocity.WithZ( 0 );
	}

	private void ApplyHalfGravity()
	{
		var halfGravity = Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;

		if ( !Character.IsOnGround )
		{
			Character.Velocity += halfGravity;
		}
		else
		{
			Character.Velocity = Character.Velocity.WithZ( 0 );
		}
	}

	private float GetAcceleration()
	{
		if ( !Character.IsOnGround ) return AirAcceleration;

		return Acceleration;
	}

	private void ApplyAcceleration()
	{
		Character.Acceleration = GetAcceleration();
	}

	/// <summary>
	/// Handles jump movement
	/// </summary>
	private void ApplyJump()
	{
		if ( Character.IsOnGround && Input.Pressed( "Jump" ) )
		{
			Character.Punch( Vector3.Up * GetJumpPower() );
			BroadcastJump();
		}
	}

	/// <summary>
	/// Broadcasts a jump event to everyone in the game.
	/// </summary>
	[Broadcast]
	private void BroadcastJump()
	{
		if ( AnimationHelper.IsValid() )
		{
			AnimationHelper.TriggerJump();
		}
	}

	private float GetMaxAcceleration()
	{
		if ( !Character.IsOnGround ) return MaxAirAcceleration;
		return MaxAcceleration;
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		BuildWishVelocity();
		ApplyAcceleration();
		ApplyJump();

		Character.ApplyFriction( GetFriction() );

		if ( Character.IsOnGround )
		{
			Character.Velocity = Character.Velocity.WithZ( 0 );
		}
		else
		{
			// Apply half of the gravity
			ApplyHalfGravity();
		}

		Character.Accelerate( WishVelocity.ClampLength( GetMaxAcceleration() ) );

		Character.Move();
		// Apply the second half
		ApplyHalfGravity();

		var voxel = Scene.GetAllComponents<VoxelComponent>().FirstOrDefault();
		if ( voxel.IsValid() )
		{
			var face = voxel.GetBlockInDirection( Scene.Camera.Transform.Position * ( 1.0f / Chunk.VoxelSize ), Scene.Camera.Transform.Rotation.Forward, 1000, out var p, out var distance );
			if ( face != VoxelComponent.BlockFace.Invalid )
			{
				if ( Input.Down( "attack1" ) )
				{
					var b = Game.Random.Int( 1, 5 );
					voxel.SetBlockAndUpdate( VoxelComponent.GetAdjacentBlockPosition( p, (int)face ), (byte)b );
				}
			}
		}
	}

	private void ApplyAnimation()
	{
		if ( AnimationHelper.IsValid() )
		{
			AnimationHelper.WithVelocity( Character.Velocity );
			AnimationHelper.WithWishVelocity( WishVelocity );
			AnimationHelper.IsGrounded = Character.IsOnGround;
			AnimationHelper.WithLook( EyeAngles.Forward, 0.1f, 0.1f, 0.1f );
			AnimationHelper.DuckLevel = 0;
			AnimationHelper.HoldType = HoldType;
			AnimationHelper.Handedness = CitizenAnimationHelper.Hand.Right;
			AnimationHelper.AimBodyWeight = 0.1f;
		}
	}

	protected override void OnUpdate()
	{
		// Only rotate the model if we're in motion
		if ( ModelRenderer.IsValid() && WishMove.Length > 0f )
		{
			ModelRenderer.Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );
		}

		ApplyAnimation();

		if ( IsProxy )
			return;

		CameraController.UpdateFromPlayer();
	}

	/// <summary>
	/// Called when you die and respawn, at the moment it's immediate
	/// </summary>
	public void Respawn()
	{
		HealthComponent.Health = HealthComponent.MaxHealth;

		// This is from the host, so we gotta tell the owner that we want them to respawn.
		// also means we can do stuff on other clients, informing them
		BroadcastRespawn();
	}

	[Broadcast]
	private void BroadcastRespawn()
	{
		Transform.Position = Vector3.Zero;
	}

	/// <summary>
	/// Called on the host when damaging smoething
	/// </summary>
	/// <param name="damage"></param>
	void IDamage.OnDamage( DamageInstance damage )
	{
	}

	/// <summary>
	/// Called on the host when this thing has been killed
	/// </summary>
	/// <param name="eventArgs"></param>
	void IGameEventHandler<KilledEvent>.OnGameEvent( KilledEvent eventArgs )
	{
		Respawn();
	}
}
