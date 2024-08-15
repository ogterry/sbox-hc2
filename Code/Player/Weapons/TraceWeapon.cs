using Sandbox.Events;
using HC2;
using Voxel;


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

	LineRenderer lineRenderer;
	Vector3 lastTraceEnd;

	TimeSince timeSinceAttack;
	TimeSince ammoConsumptionRate;

	protected override void OnPreRender()
	{
		base.OnPreRender();

		UpdateLaser();
	}

	protected override bool CanAttack()
	{
		return FireConstantly || (timeSinceAttack >= AttackDelay);
	}

	protected override void Attack()
	{
		var tr = Scene.Trace
			.Ray( Scene.Camera.Transform.Position, Scene.Camera.Transform.Position + Scene.Camera.Transform.Rotation.Forward * MaxTraceDistance )
			.WithoutTags( "player", "trigger", "bodypart", "worlditem" )
			.Run();

		lastTraceEnd = tr.EndPosition;
		if ( tr.Hit )
		{
			lastTraceEnd = tr.HitPosition;
		}

		DrawLaser();

		if ( FireConstantly && timeSinceAttack < AttackDelay )
			return;

		if ( AmmoConsumptionRate.HasValue )
		{
			if ( ammoConsumptionRate > AmmoConsumptionRate.Value )
			{
				ammoConsumptionRate = 0;
				ConsumeAmmo();
			}
		}

		if ( timeSinceAttack > AttackDelay )
		{
			if ( tr.Hit )
			{
				if ( !AmmoConsumptionRate.HasValue )
					ConsumeAmmo();

				if ( tr.Shape.Tags.Has( "voxel" ) )
				{
					BroadcastDamageWorld( tr.HitPosition, tr.Direction, Damage );
				}
				else
				{
					foreach ( var damageable in tr.GameObject.Root.Components.GetAll<HealthComponent>() )
					{
						damageable.TakeDamage( ConstructDamage( tr ) );
					}
				}

				GameObject.Root.Dispatch( new ItemUseEvent( DurabilityOnUse ) );
			}
			timeSinceAttack = 0;
		}
	}

	void ConsumeAmmo()
	{
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
	}

	void DrawLaser()
	{
		if ( LaserVisible )
		{
			if ( !lineRenderer.IsValid() )
			{
				lineRenderer = Components.Create<LineRenderer>();
				lineRenderer.Color = LaserColor;
				lineRenderer.UseVectorPoints = true;
			}

			lineRenderer.Width = LaserWidth;
			lineRenderer.VectorPoints = new List<Vector3>()
			{
				Muzzle.Transform.Position,
				lastTraceEnd
			};
		}
		else if ( lineRenderer.IsValid() )
		{
			lineRenderer.Destroy();
		}
	}

	void UpdateLaser()
	{
		if ( !LaserVisible ) return;
		if ( !lineRenderer.IsValid() ) return;

		if ( FireConstantly )
		{
			lineRenderer.VectorPoints = new List<Vector3>()
			{
				Muzzle.Transform.Position,
				lastTraceEnd
			};
		}

		if ( !Input.Down( InputAction ) )
		{
			lineRenderer.Width = lineRenderer.Width.Evaluate( 0 ).LerpTo( 0, Time.Delta * 10f );
			if ( lineRenderer.Width.Evaluate( 0 ) < 0.05f )
			{
				lineRenderer.Destroy();
			}
		}
	}

	[Broadcast]
	private void BroadcastDamageWorld( Vector3 pos, Vector3 dir, float damage )
	{
		Scene.Dispatch( new DamageWorldEvent( pos, dir, GameObject, damage ) );
	}
}
