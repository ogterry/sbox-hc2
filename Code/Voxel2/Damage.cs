
using System;
using Sandbox.Events;

namespace Voxel;

partial class VoxelRenderer
	: IGameEventHandler<DamageWorldEvent>
{
	void IGameEventHandler<DamageWorldEvent>.OnGameEvent( DamageWorldEvent eventArgs )
	{
		if ( Model is null ) return;

		var coords = WorldToVoxelCoords( eventArgs.Damage.Position );

		if ( Model.OutOfBounds( coords ) ) return;

		for ( var dx = -1; dx <= 1; ++dx )
		for ( var dy = -1; dy <= 1; ++dy )
		for ( var dz = -1; dz <= 1; ++dz )
		{
			// Cut off corners randomly
			if ( Math.Abs( dx ) + Math.Abs( dy ) + Math.Abs( dz ) == 3 && Random.Shared.NextSingle() < 0.5f )
			{
				continue;
			}

			Model.AddVoxel( coords.x + dx, coords.y + dy, coords.z + dz, 0 );
		}

		Model.SetRegionDirty( coords - 1, coords + 1);
		MeshChunks();
	}
}
