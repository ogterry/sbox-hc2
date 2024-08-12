public partial class ProjectileWeapon : WeaponComponent
{
	/// <summary>
	/// What projectile should we spawn?
	/// </summary>
	[Property] public GameObject ProjectilePrefab { get; set; }

	/// <summary>
	/// Where's the muzzle of this gun?
	/// </summary>
	[Property] public GameObject Muzzle { get; set; }

	/// <summary>
	/// How fast should the projectile be travelling?
	/// </summary>
	[Property] public float EjectSpeed { get; set; } = 1024000f;

	/// <summary>
	/// The shoot sound
	/// </summary>
	[Property] public SoundEvent Sound { get; set; }

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

		var projectile = go.Components.Get<Projectile>();
		var rb = go.Components.Get<Rigidbody>();
		if ( !projectile.IsValid() || !rb.IsValid() )
		{
			Log.Warning( $"Tried to create a Projectile from {this}, but something went wrong. Projectile: {projectile}, Rigidbody: {rb}" );
			go.Destroy();
			return;
		}

		projectile.Weapon = this;
		projectile.Damage = GetDamage();
		projectile.DamageType = GetDamageType();

		// Go far
		rb.ApplyForce( Player.CameraController.AimRay.Forward * EjectSpeed );

		// Spawn over the network using the player as the owner
		go.NetworkSpawn( Player.Network.OwnerConnection );
	}
}
