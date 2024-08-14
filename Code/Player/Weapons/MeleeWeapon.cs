	using HC2;
	using Sandbox.Events;

	public partial class MeleeWeapon : WeaponComponent
{
	[Property, Group( "Melee" )] 
	public float AttackRange { get; set; } = 512f;

	[Property, Group( "Melee" )]
	public float Thickness { get; set; } = 5f;

	[Property, Group( "Melee" )]
	public float SwingDelay { get; set; } = 0.25f;

	[Sync] TimeUntil TimeUntilAttackHit { get; set; }
	[Sync] bool IsSwinging { get; set; }

	/// <summary>
	/// This is a method because the player could have buffs / stats that increase their attack range.
	/// </summary>
	/// <returns></returns>
	private float GetAttackRange()
	{
		return AttackRange;
	}

	/// <summary>
	/// The shoot sound
	/// </summary>
	[Property, Group( "Audio" )] public SoundEvent HitSound { get; set; }

	/// <summary>
	/// The shoot sound
	/// </summary>
	[Property, Group( "Effects" )] public GameObject PrefabToSpawn { get; set; }

	[Broadcast]
	private void BroadcastHitEffects( Vector3 position )
	{
		if ( HitSound is not null )
		{
			Sound.Play( HitSound, position );
		}

		if ( PrefabToSpawn.IsValid() )
		{
			PrefabToSpawn.Clone( position );
		}
	}

	protected override bool CanAttack()
	{
		if ( IsSwinging ) return false;

		return base.CanAttack();
	}

	protected override void Attack()
	{
		base.Attack();

		TimeUntilAttackHit = SwingDelay;
		IsSwinging = true;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( IsSwinging && TimeUntilAttackHit )
		{
			IsSwinging = false;

			Log.Info( "Swing!" );

			var tr = Scene.Trace.Ray( GameObject.Transform.Position, GameObject.Transform.Position + Player.CameraController.AimRay.Forward * GetAttackRange() )
				.IgnoreGameObjectHierarchy( Player.GameObject )
				.Size( Thickness )
				.Run();

			if ( tr.Hit )
			{
				if ( tr.Shape.Tags.Has( "voxel" ) )
				{
					Scene.Dispatch( new DamageWorldEvent( ConstructDamage( tr ) ) );
					return;
				}

				foreach ( var damageable in tr.GameObject.Root.Components.GetAll<HealthComponent>() )
				{
					damageable.TakeDamage( ConstructDamage( tr ) );
				}

				BroadcastHitEffects( tr.EndPosition );
			}
		}
	}
}
