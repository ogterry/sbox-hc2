using Sandbox.Events;
using Voxel;

namespace HC2;

public sealed class DamageTrigger : Component, Component.ITriggerListener
{
    [Property] public DamageType DamageType { get; set; } = DamageType.Blunt;
    [Property] public float Damage { get; set; } = 10f;
    [Property] public float Knockback { get; set; } = 0f;
    [Property] public bool CanDamageWorld { get; set; } = false;
    [Property] public bool CanDamagePlayers { get; set; } = true;
    [Property] public bool CanDamageMobs { get; set; } = true;

    public void OnTriggerEnter( Collider other )
    {
        if ( other.GameObject.Root == GameObject.Root )
            return;

        var force = Transform.Position - other.Transform.Position;
        force = force.Normal * Knockback;

        if ( CanDamageWorld && other.Tags.Has( "voxel" ) )
        {
            using ( Rpc.FilterInclude( Connection.Host ) )
            {
                BroadcastDamageWorld( Transform.Position, force.Normal, Damage );
            }
        }

        var healthComponent = other.GameObject?.Root.Components.Get<HealthComponent>();
        if ( healthComponent.IsValid() )
        {
            if ( (CanDamagePlayers && healthComponent.Tags.Has( "player" )) || (CanDamageMobs && healthComponent.Tags.Has( "mob" )) )
            {
                healthComponent.TakeDamage( new DamageInstance()
                {
                    Attacker = this,
                    Inflictor = this,
                    Damage = Damage,
                    Type = DamageType,
                    Force = force,
                    Position = other.Transform.Position + Vector3.Up * 16f,
                    Victim = healthComponent
                } );
            }
        }
    }

    [Broadcast]
    private void BroadcastDamageWorld( Vector3 pos, Vector3 dir, float damage )
    {
        Scene.Dispatch( new DamageWorldEvent( pos, dir, damage ) );
    }
}