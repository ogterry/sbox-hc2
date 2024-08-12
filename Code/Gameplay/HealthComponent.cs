using Sandbox.Events;
using System.Text.Json.Serialization;

public record DamageTakenEvent( DamageInstance Instance ) : IGameEvent;
public record KilledEvent( DamageInstance LastDamage ) : IGameEvent;

/// <summary>
/// Event dispatched on the host when something takes damage, so it can be modified.
/// </summary>
public class ModifyDamageEvent : IGameEvent
{
	public DamageInstance DamageInstance { get; set; }

	/// <summary>
	/// Clears all health and armor damage from this event.
	/// </summary>
	public void ClearDamage()
	{
		DamageInstance = DamageInstance with { Damage = 0f };
	}

	/// <summary>
	/// Scales health damage by the given multiplier.
	/// </summary>
	public void ScaleDamage( float scale )
	{
		DamageInstance = DamageInstance with { Damage = DamageInstance.Damage * scale };
	}

	/// <summary>
	/// Adds a flag to this damage event.
	/// </summary>
	/// <param name="flag"></param>
	public void AddFlag( DamageFlags flag )
	{
		DamageInstance = DamageInstance with { Flags = DamageInstance.Flags | flag };
	}

	/// <summary>
	/// Removes a flag from this damage event.
	/// </summary>
	/// <param name="flag"></param>
	public void WithoutFlag( DamageFlags flag )
	{
		DamageInstance = DamageInstance with { Flags = DamageInstance.Flags & flag };
	}

	public void SetType( DamageType type )
	{
		DamageInstance = DamageInstance with { Type = type };
	}
}

public sealed class HealthComponent : Component
{
	[Sync, Property, JsonIgnore, ReadOnly] public float Health { get; set; } = 100f;
	[Property] public float MaxHealth { get; set; } = 100f;
	[Property] public GameObject DamageEffectPrefab { get; set; }

	public DamageInstance LastDamage { get; private set; }

	protected override void OnStart()
	{
		Health = MaxHealth;
	}

	[Broadcast]
	private void BroadcastDamage( float damage, Vector3 position, Vector3 force, Component attacker, Component inflictor = null, Component victim = null, DamageFlags flags = default, DamageType type = default )
	{
		var instance = new DamageInstance()
		{
			Damage = damage,
			Force = force,
			Victim = victim,
			Inflictor = inflictor,
			Flags = flags,
			Type = type,
			Position = position,
			Attacker = attacker, 
		};

		Log.Info( instance );

		GameObject.Dispatch( new DamageTakenEvent( instance ) );

		foreach ( var damageable in Components.GetAll<IDamage>() )
		{
			damageable.OnDamage( instance );
		}

		CreateDamageEffect( instance );
	}

	private void CreateDamageEffect( DamageInstance instance )
	{
		if ( DamageEffectPrefab == null ) return;

		var particle = DamageEffectPrefab.Clone();
		particle.Transform.Position = instance.Position;

		var textRenderer = particle.Components.Get<ParticleTextRenderer>();
		if ( textRenderer != null )
		{
			textRenderer.Text = new TextRendering.Scope( instance.Type.GetIcon() + instance.Damage.ToString(), instance.Type.GetColor(), 32, weight: 800 );
		}
	}

	private DamageInstance ModifyDamage( DamageInstance instance )
	{
		var modifyEvent = new ModifyDamageEvent()
		{
			DamageInstance = instance
		};

		GameObject.Dispatch( modifyEvent );

		return modifyEvent.DamageInstance;
	}

	public void Kill()
	{
		Health = 0;
		GameObject.Dispatch( new KilledEvent( LastDamage ) );
	}

	public void TakeDamage( DamageInstance damage )
	{
		if ( IsProxy )
			return;
		
		damage = ModifyDamage( damage );

		Health -= damage.Damage;
		LastDamage = damage;

		if ( Health <= 0 )
		{
			Kill();
		}

		BroadcastDamage( damage.Damage, damage.Position, damage.Force, damage.Attacker, damage.Inflictor, damage.Victim, damage.Flags, damage.Type );
	}
}
