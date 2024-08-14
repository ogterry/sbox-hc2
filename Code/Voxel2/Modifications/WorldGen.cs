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

	private static Transform GetRandomNoiseTransform( Random random, float scale )
	{
		var pos = new Vector3(
			random.Float( -32_768f, 32_768f ),
			random.Float( -32_768f, 32_768f ),
			random.Float( -32_768f, 32_768f ) );

		var rotation = random.Rotation();

		return new Transform( pos, rotation, scale );
	}

	public void Apply( Chunk chunk )
	{
		var min = chunk.WorldMin;
		var random = new Random( Seed );

		var surfaceNoiseTransform = GetRandomNoiseTransform( random, 0.4f );
		var biomeNoiseTransform = GetRandomNoiseTransform( random, 0.1f );
		var caveNoiseTransform = GetRandomNoiseTransform( random, 1f );

		chunk.Clear();

		var voxels = chunk.Voxels;

		for ( var x = 0; x < Constants.ChunkSize; x++ )
		{
			for ( var z = 0; z < Constants.ChunkSize; z++ )
			{
				var worldX = min.x + x;
				var worldZ = min.z + z;

				var surfaceNoisePos = surfaceNoiseTransform.PointToWorld( new Vector3( worldX, 0f, worldZ ) );
				var surfaceNoise = Math.Clamp( Noise.Fbm( 4, surfaceNoisePos.x, surfaceNoisePos.y, surfaceNoisePos.z ), 0f, 1f );
				var biomeNoisePos = biomeNoiseTransform.PointToWorld( new Vector3( worldX, 0f, worldZ ) );
				var biomeNoise = Math.Clamp( Noise.Fbm( 3, biomeNoisePos.x, biomeNoisePos.y, biomeNoisePos.z ), 0f, 1f );

				surfaceNoise = MathF.Pow( surfaceNoise, 2f + biomeNoise * 2f );

				var minHeight = 16f + biomeNoise * 32f;
				var maxHeight = 24f + biomeNoise * 256f;

				var surfaceHeight = Math.Clamp( (int)(surfaceNoise * (maxHeight - minHeight)) + minHeight, minHeight, maxHeight - 1 ) - min.y;

				var i = Chunk.WorldToLocal( x );
				var k = Chunk.WorldToLocal( z );

				var minJ = int.MaxValue;
				var maxJ = int.MinValue;

				for ( var y = 0; y < Constants.ChunkSize && y < surfaceHeight; y++ )
				{
					var worldY = min.y + y;

					var caveNoisePos = caveNoiseTransform.PointToWorld( new Vector3( worldX, worldY, worldZ ) );
					var caveNoise = Noise.Perlin( caveNoisePos.x, caveNoisePos.y, caveNoisePos.z );

					if ( caveNoise < biomeNoise * 0.4f )
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

					var j = Chunk.WorldToLocal( y );

					voxels[Chunk.GetAccessLocal( i, j, k )] = blockType;

					minJ = Math.Min( j, minJ );
					maxJ = Math.Max( j, maxJ );
				}

				if ( maxJ < 0 || minJ > 255 ) continue;

				var heightmapAccess = Chunk.GetHeightmapAccess( i, k );

				chunk.MinAltitude[heightmapAccess] = (byte)minJ;
				chunk.MaxAltitude[heightmapAccess] = (byte)maxJ;
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
		Regenerate();
	}
}
