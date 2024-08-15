using System;

namespace HC2;

public sealed class MobGib : Component, Component.ICollisionListener
{
    [RequireComponent]
    public ModelRenderer ModelRenderer { get; set; }

    [Property]
    public SoundEvent Sound { get; set; }

    TimeSince timeSinceCreated = 0;
    int collisions = Random.Shared.Int( 1, 3 );

    protected override void OnFixedUpdate()
    {
        if ( timeSinceCreated > 2f )
        {
            Break();
        }
    }

    public void OnCollisionStart( Collision collision )
    {
        if ( collision.Other.GameObject?.Tags?.Has( "bodypart" ) ?? false ) return;
        if ( collision.Other.GameObject?.Tags?.Has( "projectile" ) ?? false ) return;

        collisions--;
        if ( collisions > 0 ) return;

        Break();
    }

    void Break()
    {
        var killPrefab = ResourceLibrary.Get<PrefabFile>( "prefabs/particles/KillEffect.prefab" );
        var killParticle = SceneUtility.GetPrefabScene( killPrefab ).Clone( Transform.Position );

        var material = ModelRenderer.Model.Materials.FirstOrDefault();
        var particleRenderer = killParticle.Components.Get<ParticleModelRenderer>( FindMode.EnabledInSelfAndChildren );
        particleRenderer.MaterialOverride = material;

        var sound = Sandbox.Sound.Play( Sound, Transform.Position );
        if ( sound.IsValid() )
        {
            sound.Pitch = Random.Shared.Float( 0.3f, 0.5f );
            sound.Volume = Random.Shared.Float( 0.5f, 0.8f );
        }

        GameObject.Destroy();
    }
}