
using System;
using HC2;
using Sandbox.Events;
using Voxel.Modifications;

namespace Voxel;

public record DamageWorldEvent( Vector3 Position, Vector3 Direction, GameObject Inflictor, float Damage ) : IGameEvent;

[Icon( "healing" )]
public sealed class VoxelDamage : Component,
	IGameEventHandler<DamageWorldEvent>
{
	[RequireComponent] public VoxelRenderer Renderer { get; private set; }
	[RequireComponent] public VoxelNetworking VoxelNetworking { get; private set; }

	void IGameEventHandler<DamageWorldEvent>.OnGameEvent( DamageWorldEvent eventArgs )
	{
		if ( IsProxy ) return;

		var coords = Renderer.WorldToVoxelCoords( eventArgs.Position );

		if ( Renderer.Model.OutOfBounds( coords ) ) return;

		// TODO: maybe go with ToolType instead xD

		foreach ( var gatherer in eventArgs.Inflictor.Components.GetAll<ResourceGatherer>( FindMode.EverythingInSelf ) )
		{
			// TODO: influence radius / damage with effectiveness

			VoxelNetworking.Modify( new CarveModification( gatherer.SourceKind, 1, coords, 1, Random.Shared.Next() ) );
		}
	}
}
