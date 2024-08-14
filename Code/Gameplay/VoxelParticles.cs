using Sandbox;
using Voxel;

namespace HC2;

public static class VoxelParticles
{
	static GameObject SpawnParticle( Vector3 position )
	{
		var killPrefab = ResourceLibrary.Get<PrefabFile>( "prefabs/particles/VoxelEffect.prefab" );
		return SceneUtility.GetPrefabScene( killPrefab ).Clone( position );
	}

	public static void Spawn( Vector3 position, int count )
	{
		var objs = Game.ActiveScene.FindInPhysics( new Sphere( position, 16f ) );
		var renderer = objs.SelectMany( x => x.Components.GetAll<ModelRenderer>() ).Where( x => !(x.Tags.Has( "projectile" ) || x.Tags.Has( "player" )) ).OrderByDescending( x => x.Transform.Position.DistanceSquared( position ) ).FirstOrDefault();
		if ( renderer == null )
		{
			var voxelRenderer = Game.ActiveScene.GetAllComponents<VoxelRenderer>().FirstOrDefault( x => x.Tags.Has( "terrain" ) );
			if ( voxelRenderer != null )
			{
				var voxelPos = voxelRenderer.WorldToVoxelCoords( position );
				var voxel = voxelRenderer.Model.GetVoxel( voxelPos.x, voxelPos.y, voxelPos.z );
				var palette = voxelRenderer.Palette;
				var paletteMat = palette.Materials[voxel];

				Log.Info( paletteMat );

				// var mat = Material.Create( "voxel_particle", "shaders/pixel_shader.shader" );
				// mat.Set( "Color", paletteMat.Texture );
				var mat = Material.Create( "voxel_particle", "complex.shader" );
				mat.Set( "Color", paletteMat.Texture );
				Spawn( position, mat, count );

				return;
			}

			Spawn( position, Color.White, count );
			return;
		}

		Log.Info( renderer.GameObject );

		var material = renderer.MaterialOverride ?? renderer.Model.Materials.FirstOrDefault();
		Spawn( position, material, renderer.Tint, count );
	}

	public static void Spawn( Vector3 position, Material material, int count )
	{
		var obj = SpawnParticle( position );

		var particleRenderer = obj.Components.Get<ParticleModelRenderer>( FindMode.EnabledInSelfAndChildren );
		particleRenderer.MaterialOverride = material;

		var particleEffect = obj.Components.Get<ParticleEffect>( FindMode.InChildren );
		particleEffect.MaxParticles = count;
	}

	public static void Spawn( Vector3 position, Color color, int count )
	{
		var obj = SpawnParticle( position );

		var particleEffect = obj.Components.Get<ParticleEffect>( FindMode.InChildren );
		particleEffect.Tint = color;
		particleEffect.MaxParticles = count;
	}

	public static void Spawn( Vector3 position, Material material, Color color, int count )
	{
		var obj = SpawnParticle( position );

		var particleRenderer = obj.Components.Get<ParticleModelRenderer>( FindMode.EnabledInSelfAndChildren );
		particleRenderer.MaterialOverride = material;

		var particleEffect = obj.Components.Get<ParticleEffect>( FindMode.InChildren );
		particleEffect.Tint = color;
		particleEffect.MaxParticles = count;
	}
}

public sealed class NearestParticleMaterial : Component, Component.ITriggerListener
{
	[Property] ParticleEffect ParticleEffect { get; set; }
	[Property] ParticleModelRenderer ParticleRenderer { get; set; }
	[Property] Component EnableAfter { get; set; }

	public void OnTriggerEnter( Collider other )
	{
		if ( other.GameObject?.Tags.Has( "player" ) ?? false )
			return;

		if ( other.GameObject?.Tags.Has( "projectile" ) ?? false )
			return;

		if ( !other.GameObject.Components.TryGet<ModelRenderer>( out var renderer, FindMode.EnabledInSelfAndDescendants ) )
			return;

		Log.Info( other.GameObject );

		var material = renderer.MaterialOverride ?? renderer.Model.Materials.FirstOrDefault();
		ParticleRenderer.MaterialOverride = material;

		ParticleEffect.Tint = renderer.Tint;
		ParticleEffect.Gradient = renderer.Tint;

		ParticleEffect.Enabled = true;
		EnableAfter.Enabled = true;
		Enabled = false;
	}
}
