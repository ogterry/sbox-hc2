namespace HC2;

public sealed class MobGib : Component, Component.ICollisionListener
{
    [RequireComponent]
    public ModelRenderer ModelRenderer { get; set; }

    TimeSince timeSinceSpawn = 0;

    public void OnCollisionStart( Collision collision )
    {
        if ( collision.Other.GameObject?.Tags?.Has( "bodypart" ) ?? false ) return;

        var killPrefab = ResourceLibrary.Get<PrefabFile>( "prefabs/particles/KillEffect.prefab" );
        var killParticle = SceneUtility.GetPrefabScene( killPrefab ).Clone( Transform.Position );

        var material = ModelRenderer.Model.Materials.FirstOrDefault();
        var particleRenderer = killParticle.Components.Get<ParticleModelRenderer>( FindMode.EnabledInSelfAndChildren );
        particleRenderer.MaterialOverride = material;

        GameObject.Destroy();
    }
}