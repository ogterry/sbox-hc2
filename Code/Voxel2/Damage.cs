
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
			Model.AddVoxel( x + dx, y + dy, z + dz, 0 );
		}

		MeshChunks();
	}
}

