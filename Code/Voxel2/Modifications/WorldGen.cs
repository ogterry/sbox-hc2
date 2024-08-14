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

	public void Apply( Chunk chunk )
	{
		var min = chunk.WorldMin;

		const int height = 32;

		for ( var x = 0; x < Constants.ChunkSize; x++ )
		{
			for ( var z = 0; z < Constants.ChunkSize; z++ )
			{
				var worldX = min.x + x;
				var worldZ = min.z + z;

				var noiseValue = Noise.Fbm( 8, worldX * 0.4f, worldZ * 0.4f );
				var surfaceHeight = Math.Clamp( (int)(noiseValue * height), 0, height - 1 ) - min.y;

				for ( var y = 0; y < Constants.ChunkSize && y < surfaceHeight; y++ )
				{
					var worldY = min.y;
					var caveNoise = Noise.Perlin( worldX * 1.0f, worldY * 1.0f, worldZ * 1.0f );

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

					chunk.SetVoxel( z, y, x, blockType );
				}
			}
		}
	}
}
