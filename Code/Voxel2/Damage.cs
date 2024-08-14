
using System;
using Sandbox.Events;

namespace Voxel;

partial class VoxelRenderer
	: IGameEventHandler<DamageWorldEvent>
{
	void IGameEventHandler<DamageWorldEvent>.OnGameEvent( DamageWorldEvent eventArgs )
	{
		if ( Model is null ) return;

		var (x, y, z) = WorldToVoxelCoords( eventArgs.Damage.Position );

		if ( Model.OutOfBounds( x, y, z ) ) return;

		for ( var dx = -1; dx <= 1; ++dx )
		for ( var dy = -1; dy <= 1; ++dy )
		for ( var dz = -1; dz <= 1; ++dz )
		{
			// Cut off corners randomly
			if ( Math.Abs( dx ) + Math.Abs( dy ) + Math.Abs( dz ) == 3 && Random.Shared.NextSingle() < 0.5f )
			{
				continue;
			}

			Model.AddVoxel( x + dx, y + dy, z + dz, 0 );
		}

		Model.SetRegionDirty( x - 1, y - 1, z - 1, x + 1, y + 1, z + 1 );
		MeshChunks();
	}
}

