
using System;
using Sandbox.Diagnostics;

namespace Voxel.Modifications;

#nullable enable

public record HeightmapModification( Vector3Int Min, Vector3Int Max, HeightmapSample[] Heightmap ) : IModification
{
	private record struct CompressedSample( byte Height, byte Terrain )
	{
		public static implicit operator HeightmapSample( CompressedSample compressed )
		{
			return new HeightmapSample( compressed.Height, compressed.Terrain / 255f );
		}

		public static explicit operator CompressedSample( HeightmapSample sample )
		{
			return new CompressedSample(
				(byte)Math.Clamp( sample.Height, 0, 255 ),
				(byte)Math.Clamp( (int)MathF.Round( sample.Terrain * 255f ), 0, 255 ) );
		}
	}

	private static HeightmapSample[] Decompress( ReadOnlySpan<CompressedSample> compressed )
	{
		// TODO: RLE?

		var result = new HeightmapSample[compressed.Length];

		for ( var i = 0; i < compressed.Length; ++i )
		{
			result[i] = compressed[i];
		}

		return result;
	}

	private static ReadOnlySpan<CompressedSample> Compress( HeightmapSample[] samples )
	{
		// TODO: RLE?

		var result = new CompressedSample[samples.Length];

		for ( var i = 0; i < samples.Length; ++i )
		{
			result[i] = (CompressedSample)samples[i];
		}

		return result;
	}

	public ModificationKind Kind => ModificationKind.Heightmap;

	public HeightmapModification( ByteStream stream, Vector3Int min, Vector3Int max )
		: this(
			min, max,
			Decompress( stream.ReadArray<CompressedSample>( (max.x - min.x) * (max.z - min.z) ) ) )
	{

	}

	[ThreadStatic]
	private static HeightmapSample[]? _chunkHeightmap;

	private void GetChunkHeightMap( int minX, int minZ, Span<HeightmapSample> output )
	{
		Assert.True( output.Length >= Constants.ChunkSizeSquared );
		Assert.True( minX >= Min.x );
		Assert.True( minZ >= Min.z );
		Assert.True( minX + Constants.ChunkSize <= Max.x );
		Assert.True( minZ + Constants.ChunkSize <= Max.z );

		var stride = Max.x - Min.x;
		var baseIndex = minX - Min.x + (minZ - Min.z) * stride;

		for ( var z = 0; z < Constants.ChunkSize; ++z )
		{
			var index = baseIndex + z * stride;

			Heightmap
				.AsSpan( index, Constants.ChunkSize )
				.CopyTo( output.Slice( z << Constants.ChunkShift, Constants.ChunkSize ) );
		}
	}

	private bool IsChunkPopulated( Vector3Int chunkMin, Span<HeightmapSample> chunkHeightmap )
	{
		var maxHeight = 0;

		for ( var i = 0; i < Constants.ChunkSizeSquared; ++i )
		{
			maxHeight = Math.Max( maxHeight, chunkHeightmap[i].Height );
		}

		return maxHeight > chunkMin.y;
	}

	public bool ShouldCreateChunk( Vector3Int chunkMin )
	{
		var chunkHeightmap = _chunkHeightmap ??= new HeightmapSample[Constants.ChunkSizeSquared];

		// Span<HeightmapSample> chunkHeightmap = stackalloc HeightmapSample[Constants.ChunkSizeSquared];

		GetChunkHeightMap( chunkMin.x, chunkMin.z, chunkHeightmap );

		return IsChunkPopulated( chunkMin, chunkHeightmap );
	}

	public void Write( ref ByteStream stream )
	{
		stream.WriteArray( Compress( Heightmap ) );
	}

	public void Apply( VoxelRenderer renderer, Chunk chunk )
	{
		var chunkHeightmap = _chunkHeightmap ??= new HeightmapSample[Constants.ChunkSizeSquared];
		// Span<HeightmapSample> chunkHeightmap = stackalloc HeightmapSample[Constants.ChunkSizeSquared];

		var min = chunk.WorldMin;

		GetChunkHeightMap( min.x, min.z, chunkHeightmap );

		if ( !IsChunkPopulated( min, chunkHeightmap ) )
		{
			chunk.Deallocate();
			return;
		}

		chunk.Clear();

		var voxels = chunk.Voxels;
		var biomeSampler = renderer.Components.Get<BiomeSampler>();
		var palette = renderer.Palette;

		for ( var x = 0; x < Constants.ChunkSize; x++ )
		{
			for ( var z = 0; z < Constants.ChunkSize; z++ )
			{
				var i = Chunk.WorldToLocal( x );
				var k = Chunk.WorldToLocal( z );

				var heightmapAccess = Chunk.GetHeightmapAccess( i, k );

				var (height, terrain) = chunkHeightmap[heightmapAccess];
				var biome = biomeSampler?.GetBiomeAt( min.x + x, min.z + z );

				var minJ = int.MaxValue;
				var maxJ = int.MinValue;

				var localHeight = height - min.y;

				for ( var y = 0; y < Constants.ChunkSize && y < localHeight; y++ )
				{
					byte blockIndex = 1;

					if ( biome?.GetBlock( height, terrain, localHeight - y - 1 ) is { } block )
					{
						blockIndex = palette.GetBlockIndex( block, block.MaxHealth );
					}

					var j = Chunk.WorldToLocal( y );

					voxels[Chunk.GetAccessLocal( i, j, k )] = blockIndex;

					minJ = Math.Min( j, minJ );
					maxJ = Math.Max( j, maxJ );
				}

				if ( maxJ < 0 || minJ > 255 ) continue;

				chunk.MinAltitude[heightmapAccess] = (byte)minJ;
				chunk.MaxAltitude[heightmapAccess] = (byte)maxJ;
			}
		}
	}
}
