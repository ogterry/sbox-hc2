using HC2;
using Sandbox.Citizen;
using Sandbox.Events;

public partial class Player : Component, IDamage,
	IGameEventHandler<KilledEvent>,
	IGameEventHandler<ModifyDamageEvent>,
	IGameEventHandler<ItemEquipEvent>,
	IGameEventHandler<ItemUnequipEvent>,
	IGameEventHandler<ItemUseEvent>
{

	/// <summary>
	/// A reference to the local player. Returns null if one does not exist (headless server or something)
	/// </summary>
	public static Player Local
	{
		get
		{
			if ( !_local.IsValid() )
			{
				_local = Game.ActiveScene.GetAllComponents<Player>().FirstOrDefault( x => x.Network.IsOwner );
			}
			return _local;
		}
	}
	private static Player _local;

	/// <summary>
	/// The player's character controller, handles movement
	/// </summary>
	[RequireComponent]
	public CharacterController Character { get; set; }

	/// <summary>
	/// The player's health component, handles their HP
	/// </summary>
	[RequireComponent]
	public HealthComponent HealthComponent { get; set; }

	/// <summary>
	/// The player's experience component, handles their XP, levels, and upgrades stats
	/// </summary>
	[RequireComponent]
	public PlayerExperience Experience { get; set; }

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
	/// The player's hotbar
	/// </summary>
	[Property, Group( "Components" ), Sync]
	public Hotbar Hotbar { get; set; }

	/// <summary>
	/// The player's inventory
	/// </summary>
	[Property, Group( "Components" ), Sync]
	public Inventory Inventory { get; set; }

	/// <summary>
	/// The player's nametag
	/// </summary>
	[Property, Group( "Components" ), Sync]
	public Nametag Nametag { get; set; }

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

	///<summary>
	/// Base Walk speed, can possibly change
	/// </summary>
	[Property, Group( "Movement Config" )]
	public float WalkMovementSpeed { get; set; } = 128f;

	/// <summary>
	/// Base Run speed, can possibly change
	/// </summary>
	[Property, Group( "Movement Config" )]
	public float RunMovementSpeed { get; set; } = 360f;

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

	///<summary>
	/// Duck Height
	/// </summary>
	[Property, Group( "Movement Config" )]
	public float DuckHeight { get; set; } = 36f;

	///<summary>
	///Duck Speed
	/// </summary>
	[Property, Group( "Movement Config" )]
	public float DuckSpeed { get; set; } = 128f;

	/// <summary>
	/// The player's character name
	/// </summary>
	[Sync]
	public string CharacterName { get; set; } = "Player";

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
	private Vector3 WishMove;

	/// <summary>
	/// How sticky are we to the ground?
	/// </summary>
	private float Friction { get; set; } = 10;

	/// <summary>
	/// What's our holdtype?
	/// </summary>
	public CitizenAnimationHelper.HoldTypes HoldType { get; set; }

	RealTimeSince timeSinceLastSave = 0;

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

	[Sync]
	bool IsDucking { get; set; }

	[Sync]
	bool IsWalking { get; set; }

	/// <summary>
	/// How fast can we move?
	/// I've made this a method as I assume we'll want to adjust this with levels, skills, armors, etc.
	/// </summary>
	/// <returns></returns>
	private Vector3 GetWishSpeed()
	{
		if ( Character.GroundObject?.Tags?.Has( "water" ) ?? false )
		{
			return MovementSpeed * 0.6f;
		}
		if ( IsDucking )
			return DuckSpeed;
		else if ( IsWalking )
			return WalkMovementSpeed;
		else if ( Input.Down( "run" ) )
			return RunMovementSpeed;
		return MovementSpeed;
	}

	///<summary>
	/// Get the Hight
	/// </summary>
	public float GetDuckHeight()
	{
		if ( IsDucking )
			return DuckHeight;
		return 72f;
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

		if ( timeSinceLastSave > 2.5f )
		{
			timeSinceLastSave = 0f;
			CharacterSave.Current?.Save( Player.Local );
		}

		DoFlyMode();

		if ( FlyMode ) return;

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

		IsDucking = Input.Down( "duck" ) || Character.TraceDirection( Vector3.Up * 72 ).Hit;

		IsWalking = Input.Down( "walk" );

		Character.Height = IsDucking ? DuckHeight : 72f;

		Character.Accelerate( WishVelocity.ClampLength( GetMaxAcceleration() ) );

		Character.Move();
		// Apply the second half
		ApplyHalfGravity();
	}

	private void ApplyAnimation()
	{
		if ( AnimationHelper.IsValid() )
		{
			AnimationHelper.WithVelocity( Character.Velocity );
			AnimationHelper.WithWishVelocity( WishVelocity );
			AnimationHelper.IsGrounded = Character.IsOnGround;
			AnimationHelper.WithLook( EyeAngles.Forward, 0.1f, 0.1f, 0.1f );
			AnimationHelper.DuckLevel = MathX.LerpTo( AnimationHelper.DuckLevel, IsDucking ? 1 : 0, Time.Delta * 10.0f );
			AnimationHelper.HoldType = HoldType;
			AnimationHelper.Handedness = CitizenAnimationHelper.Hand.Right;
			AnimationHelper.AimBodyWeight = 0.1f;
			AnimationHelper.MoveStyle = IsWalking ? CitizenAnimationHelper.MoveStyles.Walk : CitizenAnimationHelper.MoveStyles.Run;
		}
	}

	protected override void OnStart()
	{
		if ( Nametag?.GameObject?.IsValid() ?? false )
		{
			Nametag.GameObject.Enabled = IsProxy;
		}

		if ( IsProxy )
			return;

		Inventory.OverflowContainer = Hotbar.Container;
		Hotbar.OverflowContainer = Inventory.Container;

		// Load the character data
		if ( CharacterSave.Current is not null )
		{
			CharacterSave.Current.Load( this );
			CharacterName = CharacterSave.Current.Name;

			// Give them some starting weapons if they didn't have them. We can remove this later
			var smgItem = Item.Create( "weapon_smg" );
			if ( !Hotbar.HasItem( smgItem ) )
			{
				Log.Info( "giving smg" );
				Hotbar.GiveItem( smgItem );
			}

			var swordItem = Item.Create( "weapon_sword" );
			if ( !Hotbar.HasItem( swordItem ) )
			{
				Hotbar.GiveItem( swordItem );
			}
		}

		// Select first slot
		Hotbar.SelectSlot( 0 );
	}

	protected override void OnUpdate()
	{
		if ( ModelRenderer.IsValid() )
		{
			if ( Character.GroundObject?.Tags?.Has( "water" ) ?? false )
			{
				// Visually move the player down when in water
				ModelRenderer.Transform.LocalPosition = Vector3.Down * 16f;
			}
			else
			{
				ModelRenderer.Transform.LocalPosition = 0;
			}
			ModelRenderer.Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );
		}

		ApplyAnimation();

		if ( IsProxy )
			return;

		if ( Game.IsEditor )
		{
			if ( Input.Pressed( "flymode" ) )
			{
				FlyMode = !FlyMode;
			}
		}

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
		Kill();
	}

	[Authority( NetPermission.HostOnly )]
	private void Kill()
	{
		BroadcastKillEffect();
		GameObject.Destroy();
	}

	[Broadcast( NetPermission.OwnerOnly )]
	private void BroadcastKillEffect()
	{
		VoxelParticles.SpawnInBounds( GameObject.GetBounds(), 150 );
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
		Log.Info( "Killed event" );
		Kill();
	}

	void IGameEventHandler<ModifyDamageEvent>.OnGameEvent( ModifyDamageEvent eventArgs )
	{
		if ( eventArgs.DamageInstance.Attacker is Player player )
		{
			// If we're being attacked by a player, scale the damage down to 0, as this game does not support friendly fire
			eventArgs.ScaleDamage( 0f );
		}
	}


	void IGameEventHandler<ItemEquipEvent>.OnGameEvent( ItemEquipEvent eventArgs )
	{
		var item = eventArgs.Item;
		if ( item is null ) return;

		var prefab = item.Resource.Prefab;
		if ( prefab is null && item.Resource.BlockType is not null )
		{
			prefab = ResourceLibrary.Get<PrefabFile>( "prefabs/weapons/block_placer.prefab" );
		}

		if ( prefab is not null )
		{
			var obj = SceneUtility.GetPrefabScene( prefab );
			var inst = SetMainHand( obj );

			if ( inst.Components.TryGet<BlockPlacer>( out var placer, FindMode.EverythingInSelfAndDescendants ) )
			{
				Log.Info( item.Resource.BlockType );
				placer.BlockType = item.Resource.BlockType;
			}
		}
	}

	void IGameEventHandler<ItemUnequipEvent>.OnGameEvent( ItemUnequipEvent eventArgs )
	{
		var item = eventArgs.Item;
		if ( item is null ) return;

		RemoveMainHand();
	}

	void IGameEventHandler<ItemUseEvent>.OnGameEvent( ItemUseEvent eventArgs )
	{
		var durabilityUsed = eventArgs.DurabilityUsed;

		if ( Hotbar.SelectedItem is not null )
		{
			Hotbar.SelectedItem.Durability -= durabilityUsed;

			if ( Hotbar.SelectedItem.Durability <= 0 )
			{
				Hotbar.TakeItemSlot( Hotbar.SelectedSlot );
			}
		}
	}

	void DoFlyMode()
	{
		if ( FlyMode )
		{
			Character.IsOnGround = false;

			Vector3 movement = Input.AnalogMove;

			float speed = 350.0f;

			if ( Input.Down( "run" ) )
			{
				speed = 750.0f;
			}

			GameObject.Transform.Position += EyeAngles.ToRotation() * movement * speed * Time.Delta;

			Character.Velocity = EyeAngles.ToRotation() * movement * speed;

			return;
		}
	}

	[Sync]
	bool FlyMode { get; set; }
}
