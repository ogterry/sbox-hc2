public partial class MeleeWeapon : WeaponComponent
{
	[Property] public float AttackRange { get; set; } = 512f;

	/// <summary>
	/// This is a method because the player could have buffs / stats that increase their attack range.
	/// </summary>
	/// <returns></returns>
	private float GetAttackRange()
	{
		return AttackRange;
	}

	protected override void Attack()
	{
		base.Attack();

		var tr = Scene.Trace.Ray( Player.CameraController.AimRay, GetAttackRange() )
			.IgnoreGameObjectHierarchy( Player.GameObject )
			.UseHitboxes()
			.Run();

		if ( tr.Hit )
		{
			foreach ( var damageable in tr.GameObject.Root.Components.GetAll<IDamage>() )
			{
				damageable.OnDamage( ConstructDamage( tr ) );
			}
		}
	}
}
