using System;
using Sandbox.Utility;

namespace Voxel.Modifications;

public record struct WorldGenModification( int Seed, Vector3Int Min, Vector3Int Max ) : IModification
{
	public ModificationKind Kind => ModificationKind.WorldGen;
	public bool CreateChunks => true;

	public WorldGenModification( ByteStream stream )
		: this( stream.Read<int>(), stream.Read<Vector3Int>(), stream.Read<Vector3Int>() )
	{

	}

	public void Write( ref ByteStream stream )
	{
		stream.Write( Seed );
		stream.Write( Min );
		stream.Write( Max );
	}

	private static Transform GetRandomNoiseTransform( Random random )
	{
		var pos = new Vector3(
			random.Float( -32_768f, 32_768f ),
			random.Float( -32_768f, 32_768f ),
			random.Float( -32_768f, 32_768f ) );

		var rotation = random.Rotation();

		return new Transform( pos, rotation );
	}

	public void Apply( Chunk chunk )
	{
		var min = chunk.WorldMin;
		var random = new Random( Seed );

		const int height = 32;

		var surfaceNoiseTransform = GetRandomNoiseTransform( random );
		var caveNoiseTransform = GetRandomNoiseTransform( random );

		chunk.Clear();

		for ( var x = 0; x < Constants.ChunkSize; x++ )
		{
			for ( var z = 0; z < Constants.ChunkSize; z++ )
			{
				var worldX = min.x + x;
				var worldZ = min.z + z;

				var surfaceNoisePos = surfaceNoiseTransform.PointToWorld( new Vector3( worldX, 0f, worldZ ) * 0.4f );
				var surfaceNoise = Noise.Fbm( 8, surfaceNoisePos.x, surfaceNoisePos.y, surfaceNoisePos.z );
				var surfaceHeight = Math.Clamp( (int)(surfaceNoise * height), 0, height - 1 ) - min.y;

				for ( var y = 0; y < Constants.ChunkSize && y < surfaceHeight; y++ )
				{
					var worldY = min.y + y;

					var caveNoisePos = caveNoiseTransform.PointToWorld( new Vector3( worldX, worldY, worldZ ) );
					var caveNoise = Noise.Perlin( caveNoisePos.x, caveNoisePos.y, caveNoisePos.z );

					if ( caveNoise < 0.4f )
						continue;

					byte blockType;

					if ( y < surfaceHeight - 4 )
					{
						blockType = 2;
					}
					else if ( y < surfaceHeight - 1 )
					{
						blockType = 1;
					}
					else
					{
						blockType = 3;
					}

					chunk.SetVoxel( x, y, z, blockType );
				}
			}
		}
	}
}

[Icon( "casino" )]
public sealed class VoxelWorldGen : Component, Component.ExecuteInEditor
{
	[RequireComponent] public VoxelNetworking VoxelNetworking { get; private set; }

	[Property]
	public int Seed { get; set; }

	[Button( "Randomize" )]
	private void Randomize()
	{
		Seed = Random.Shared.Next();
		Regenerate();
	}

	[Button( "Regenerate" )]
	private void Regenerate()
	{
		VoxelNetworking.Modify( new WorldGenModification( Seed, 0, VoxelNetworking.Renderer.Size ) );
	}

	protected override void OnEnabled()
	{
		if ( IsProxy ) return;

		Regenerate();
	}
}
