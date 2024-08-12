
using System;

namespace HC2.Mobs;

#nullable enable

public static class MobHelpers
{
	[ActionGraphNode( "hc2.mob.getnearby" ), Group( "HC2/Mobs" ), Pure]
	public static Vector3 GetNearbyPosition( Vector3 origin, float minRange = 0f, float maxRange = 512f, float maxHeight = 128f )
	{
		const int maxAttempts = 256;
		const float traceRadius = 32f;

		for ( var i = 0; i < maxAttempts; i++ )
		{
			var angle = Random.Shared.Float( 0f, MathF.PI * 2f );
			var range = MathF.Sqrt( Random.Shared.Float( minRange / maxRange, 1f ) ) * maxRange;

			var pos = origin + new Vector3( MathF.Cos( angle ), MathF.Sin( angle ), 0f ) * range;
			var ray = new Ray( pos + Vector3.Up * maxHeight, Vector3.Down );

			var trace = Game.ActiveScene.Trace
				.Ray( ray, maxHeight * 2f )
				.Radius( traceRadius )
				.UsePhysicsWorld()
				.Run();

			if ( trace is { Hit: true, StartedSolid: false } )
			{
				return trace.EndPosition - Vector3.Up * traceRadius;
			}
		}

		return origin;
	}
}
