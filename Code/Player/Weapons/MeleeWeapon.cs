using HC2;
using Sandbox.Events;
using Voxel;

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

		VoxelParticles.Spawn( position, 15 );
	}

	protected override bool CanAttack()
	{
		if ( IsSwinging ) return false;

		return base.CanAttack();
	}

	protected override void Attack()
	{
		// Not calling base.Attack because we don't wanna use durability unless we hit
		TimeSinceAttack = 0;
		BroadcastEffects();

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

			var tr = Scene.Trace.Ray( GameObject.Transform.Position, GameObject.Transform.Position + Player.CameraController.AimRay.Forward * GetAttackRange() )
				.IgnoreGameObjectHierarchy( Player.GameObject )
				.Size( Thickness )
				.Run();

			if ( tr.Hit )
			{
				if ( tr.Shape.Tags.Has( "voxel" ) )
				{
					var damage = ConstructDamage( tr );

					using ( Rpc.FilterInclude( Connection.Host ) )
					{
						BroadcastDamageWorld( tr.HitPosition + tr.Normal * 8f, tr.Normal, damage.Damage );
					}
					return;
				}

				foreach ( var damageable in tr.GameObject.Root.Components.GetAll<HealthComponent>() )
				{
					damageable.TakeDamage( ConstructDamage( tr ) );
				}

				BroadcastHitEffects( tr.EndPosition );

				// Only use durability if we hit something for melee
				GameObject.Root.Dispatch( new ItemUseEvent( DurabilityOnUse ) );
			}
		}
	}

	[Broadcast]
	private void BroadcastDamageWorld( Vector3 pos, Vector3 dir, float damage )
	{
		Scene.Dispatch( new DamageWorldEvent( pos, dir, GameObject, damage ) );
	}
}
