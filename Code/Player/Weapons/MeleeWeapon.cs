	public partial class MeleeWeapon : WeaponComponent
{
	[Property, Group( "Melee" )] 
	public float AttackRange { get; set; } = 512f;

	[Property, Group( "Melee" )]
	public float Thickness { get; set; } = 5f;

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

		var tr = Scene.Trace.Ray( GameObject.Transform.Position, GameObject.Transform.Position + Player.CameraController.AimRay.Forward * GetAttackRange() )
			.IgnoreGameObjectHierarchy( Player.GameObject )
			.Size( Thickness )
			.Run();

		if ( tr.Hit )
		{
			foreach ( var damageable in tr.GameObject.Root.Components.GetAll<HealthComponent>() )
			{
				damageable.TakeDamage( ConstructDamage( tr ) );
			}
		}
	}
}
