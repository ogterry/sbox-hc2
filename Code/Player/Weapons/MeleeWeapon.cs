public partial class MeleeWeapon : WeaponComponent
{
	[Property] public float AttackRange { get; set; } = 512f;

	[Property] public float Damage { get; set; } = 50f;

	/// <summary>
	/// This is a method because the player could have buffs / stats that increase their attack range.
	/// </summary>
	/// <returns></returns>
	private float GetAttackRange()
	{
		return AttackRange;
	}

	/// <summary>
	/// This is a method because the player could have buffs / stats that increase their damage.
	/// </summary>
	/// <returns></returns>
	private float GetDamage()
	{
		return Damage;
	}

	private DamageInstance ConstructDamage( SceneTraceResult tr )
	{
		return new DamageInstance()
		{
			Damage = GetDamage(),
			Attacker = Player.GameObject,
			Position = tr.HitPosition,
			Weapon = GameObject
		};
	}
	protected override void Attack()
	{
		base.Attack();


		var tr = Scene.Trace.Ray( Player.CameraController.AimRay, AttackRange )
			.IgnoreGameObjectHierarchy( Player.GameObject )
			.UseHitboxes()
			.Run();

		if ( tr.Hit )
		{
			foreach ( var damageable in tr.GameObject.Root.Components.GetAll<IDamageable>() )
			{
				damageable.OnDamage( ConstructDamage( tr ) );
			}
		}
	}
}
