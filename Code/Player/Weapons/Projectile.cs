using System.Text.Json.Serialization;
using Sandbox.Events;

public partial class Projectile : Component, Component.ICollisionListener
{
	/// <summary>
	/// The projectile's weapon
	/// </summary>
	[Property, Sync, JsonIgnore] public WeaponComponent Weapon { get; set; }

	/// <summary>
	/// The rigidbody
	/// </summary>
	[RequireComponent] public Rigidbody Rigidbody { get; set; }

	/// <summary>
	/// The collider we're listening to events from
	/// </summary>
	[Property] public Collider Collider { get; set; }

	/// <summary>
	/// We'll disable this renderer when it hits something
	/// </summary>
	[Property] public ModelRenderer Renderer { get; set; }

	/// <summary>
	/// Should we spawn a prefab when we hit something with the projectile?
	/// </summary>
	[Property] public GameObject PrefabToSpawn { get; set; }

	[Property] public float LifeTime { get; set; } = 5f;

	/// <summary>
	/// Passed in by a weapon, how much damage do we inflict to a damageable?
	/// </summary>
	public float Damage { get; set; }

	/// <summary>
	/// Passed in by a weapon, what's the damage type?
	/// </summary>
	public DamageType DamageType { get; set; }

	protected override void OnStart()
	{
		Tags.Add( "projectile" );
		DestroyAfter( LifeTime );
	}

	[Broadcast]
	public void SpawnEffects( Vector3 position )
	{
		if ( PrefabToSpawn.IsValid() )
		{
			PrefabToSpawn.Clone( position );
		}
	}

	public void OnCollisionStart( Collision other )
	{
		// Log.Info( $"Collided with {other.Other.GameObject}" );

		if ( IsProxy )
		{
			// Disable it locally, wait for an authoritative figure to tell you what to do.
			Renderer.Enabled = false;
			Rigidbody.Enabled = false;
			return;
		}

		var contactPoint = other.Contact.Point;
		if ( contactPoint == Vector3.Zero ) contactPoint = Transform.Position;

		if ( other.Other.Shape.Tags.Has( "voxel" ) )
		{
			var damage = new DamageInstance()
			{
				Attacker = Weapon?.Player,
				Inflictor = this,
				Damage = Damage,
				Type = DamageType,
				Force = Rigidbody.Velocity,
				Position = contactPoint + Rigidbody.Velocity.Normal * 8f
			};

			Scene.Dispatch( new DamageWorldEvent( damage ) );
		}

		var healthComponent = other.Other.GameObject?.Root.Components.Get<HealthComponent>();
		if ( healthComponent.IsValid() )
		{
			healthComponent.TakeDamage( new DamageInstance()
			{
				Attacker = Weapon?.Player,
				Inflictor = this,
				Damage = Damage,
				Type = DamageType,
				Force = Rigidbody.Velocity,
				Position = contactPoint,
				Victim = healthComponent
			} );
		}

		InternalDestroy( other.Contact.Point );
	}

	void InternalDestroy( Vector3 position = default )
	{
		if ( IsProxy ) return;

		if ( position == default )
			position = Transform.Position;

		SpawnEffects( position );
		GameObject.Destroy();
	}

	async void DestroyAfter( float time )
	{
		await GameTask.DelaySeconds( time );
		if ( !GameObject.IsValid() ) return;
		InternalDestroy();
	}
}
