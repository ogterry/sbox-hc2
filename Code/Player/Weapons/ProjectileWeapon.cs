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

	protected override void Attack()
	{
		base.Attack();

		if ( !ProjectilePrefab.IsValid() )
		{
			Log.Warning( $"{this} doesn't have a Projectile prefab" );
			return;
		}

		var go = ProjectilePrefab.Clone( new CloneConfig()
		{
			Parent = null,
			Transform = Muzzle.Transform.World.WithScale( 1f ),
			StartEnabled = true,
			Name = $"Projectile from {this}"
		} ) ;

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
	}
}
