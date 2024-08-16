using HC2;

public partial class ProjectileWeapon : WeaponComponent
{
	/// <summary>
	/// What projectile should we spawn?
	/// </summary>
	[Property, Group( "Projectile Weapon" )] public GameObject ProjectilePrefab { get; set; }

	/// <summary>
	/// Where's the muzzle of this gun?
	/// </summary>
	[Property, Group( "Projectile Weapon" )] public GameObject Muzzle { get; set; }

	/// <summary>
	/// How fast should the projectile be travelling?
	/// </summary>
	[Property, Group( "Projectile Weapon" )] public float EjectSpeed { get; set; } = 1024000f;

	/// <summary>
	/// What item should be consumed when firing? (if any)
	/// </summary>
	[Property, Group( "Projectile Weapon" )] public ItemAsset AmmoItem { get; set; }

	///<summary>
	/// Muzzle flash prefab
	/// </summary>
	[Property, Group( "Visuals" )] public GameObject MuzzleFlashPrefab { get; set; }

	[Property, Group( "Audio" )] public SoundEvent OutOfAmmoSound { get; set; }

	protected override void Attack()
	{
		if ( !ProjectilePrefab.IsValid() )
		{
			Log.Warning( $"{this} doesn't have a Projectile prefab" );
			return;
		}

		if ( AmmoItem is not null )
		{
			var ammoItem = Item.Create( AmmoItem, 1 );
			if ( Player.Hotbar.HasItem( ammoItem ) )
			{
				Player.Hotbar.TakeItem( ammoItem );
			}
			else
			{
				TimeSinceAttack = 0;
				if ( OutOfAmmoSound is not null )
					Sound.Play( OutOfAmmoSound, Transform.Position );
				return;
			}
		}

		base.Attack();

		var go = ProjectilePrefab.Clone( new CloneConfig()
		{
			Parent = null,
			Transform = Muzzle.Transform.World.WithScale( 1f ),
			StartEnabled = true,
			Name = $"Projectile from {this}"
		} );

		if ( MuzzleFlashPrefab.IsValid() )
		{
			var muzzle = MuzzleFlashPrefab.Clone( new CloneConfig()
			{
				Parent = null,
				Transform = Muzzle.Transform.World.WithScale( 1f ).WithRotation( Rotation.From( Muzzle.Transform.Rotation * Rotation.FromPitch( 90 ) ) ),
				StartEnabled = true
			} );
		}

		// Make projectiles ignore players!
		go.Tags.Add( "ignore_players" );

		var projectile = go.Components.Get<Projectile>();
		var rb = go.Components.Get<Rigidbody>();

		// Spawn over the network using the player as the owner
		go.NetworkSpawn( Player.Network.OwnerConnection );

		if ( rb.IsValid() )
		{
			// Go far
			rb.ApplyForce( Player.CameraController.AimRay.Forward * EjectSpeed );
		}

		if ( !projectile.IsValid() )
		{
			// We're gonna return, but we're gonna spawn it anyway.
			return;
		}

		projectile.Weapon = this;
		projectile.Damage = GetDamage();
		projectile.DamageType = GetDamageType();
		projectile.VoxelDamageRadius = 2; // TODO
	}
}
