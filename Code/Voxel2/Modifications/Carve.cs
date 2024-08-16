using System;
using HC2;

namespace Voxel.Modifications;

#nullable enable

public record struct GatherEffectiveness( GatherSourceKind SourceKind, float Effectiveness );

public record struct CarveModification( float Damage, Vector3Int Origin, byte Radius, int Seed,
	params GatherEffectiveness[] Effectiveness ) : IModification
{
	public ModificationKind Kind => ModificationKind.Carve;
	public Vector3Int Min => Origin - Radius;
	public Vector3Int Max => Origin + Radius;

	public CarveModification( ByteStream stream )
		: this( stream.Read<float>(), stream.Read<Vector3Int>(), stream.Read<byte>(), stream.Read<int>(),
			stream.ReadArray<GatherEffectiveness>( 32 ).ToArray() )
	{

	}

	public bool ShouldCreateChunk( Vector3Int chunkMin ) => false;

	public void Write( ref ByteStream stream )
	{
		stream.Write( Damage );
		stream.Write( Origin );
		stream.Write( Radius );
		stream.Write( Seed );
		stream.WriteArray( Effectiveness );
	}

	private float GetEffectiveness( GatherSourceKind sourceKind )
	{
		// TODO: faster lookup?

		foreach ( var entry in Effectiveness )
		{
			if ( entry.SourceKind == sourceKind ) return entry.Effectiveness;
		}

		return 0f;
	}

	public void Apply( VoxelRenderer renderer, Chunk chunk )
	{
		var min = Min;
		var max = Max;

		var localMin = Vector3Int.Max( min - chunk.WorldMin, 0 );
		var localMax = Vector3Int.Min( max - chunk.WorldMin, Constants.ChunkSize - 1 );

		var voxels = chunk.Voxels;
		var palette = renderer.Palette;

		var localCenter = (max + min) / 2 - chunk.WorldMin;

		for ( var x = localMin.x; x <= localMax.x; x++ )
		for ( var y = localMin.y; y <= localMax.y; y++ )
		for ( var z = localMin.z; z <= localMax.z; z++ )
		{
			var index = voxels[Chunk.GetAccessLocal( x, y, z )];
			if ( index == 0 ) continue;

			var distanceSq = (new Vector3Int( x, y, z ) - localCenter).LengthSquared;
			if ( distanceSq > Radius * Radius ) continue;

			var entry = palette.GetEntry( index );
			if ( entry.IsEmpty || entry.Health == 0 ) continue;

			var effectiveness = GetEffectiveness( entry.Block.MaterialKind );
			if ( effectiveness <= 0f ) continue;

			var falloff = Math.Clamp( 1f - distanceSq / (1f + Radius * Radius), 0, 1f );
			var damage = (int) Math.Round( Damage * entry.Block.DamageScale * effectiveness * falloff );
			if ( damage < 1 ) continue;

			if ( entry.Health <= damage )
			{
				chunk.SetVoxel( x, y, z, 0 );

				if ( !renderer.IsProxy )
				{
					var worldPos = renderer.VoxelToWorldCoords( new Vector3Int( x, y, z ) + chunk.WorldMin );
					entry.Block.BlockDestroyed( renderer, worldPos );
				}
			}
			else
			{
				chunk.SetVoxel( x, y, z, (byte) (index + damage) );
			}
		}
	}
}
