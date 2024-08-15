using HC2;

public partial class TraceWeapon : WeaponComponent
{

	/// <summary>
	/// Where's the muzzle of this gun?
	/// </summary>
	[Property, Group( "Trace Weapon" )] public GameObject Muzzle { get; set; }

	/// <summary>
	/// Whether or not the weapon should trace constantly while the attack button is held down.
	/// Damage will be applied at the AttackDelay interval.
	/// </summary>
	[Property, Group( "Trace Weapon" )] public bool FireConstantly { get; set; }

	[Property, Group( "Trace Weapon" )] public float MaxTraceDistance { get; set; } = 2000f;

	/// <summary>
	/// What item should be consumed when firing? (if any)
	/// </summary>
	[Property, Group( "Weapon Ammo" )] public ItemAsset AmmoItem { get; set; }

	/// <summary>
	/// How often ammo should be consumed. This will override the FireRate if set. 
	/// </summary>
	[Property, Group( "Weapon Ammo" )] public float? AmmoConsumptionRate { get; set; } = 0;

	[Property, Group( "Trace Laser" )] public bool LaserVisible { get; set; }
	[Property, Group( "Trace Laser" )] public float LaserWidth { get; set; } = 2f;
	[Property, Group( "Trace Laser" )] public Color LaserColor { get; set; } = Color.Red;

	///<summary>
	/// Muzzle flash prefab
	/// </summary>
	[Property, Group( "Visuals" )] public GameObject MuzzleFlashPrefab { get; set; }

	[Property, Group( "Audio" )] public SoundEvent OutOfAmmoSound { get; set; }

	protected override void Attack()
	{

	}
}
