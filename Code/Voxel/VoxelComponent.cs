using Sandbox;
using System;

namespace HC2;

public partial class VoxelComponent : Component, Component.ExecuteInEditor
{
	[Property, Range( 1, 512 ), MakeDirty]
	public int SizeX { get; set; }

	[Property, Range( 1, 512 ), MakeDirty]
	public int SizeY { get; set; }

	[Property, Range( 1, 512 ), MakeDirty]
	public int SizeZ { get; set; }

	private Chunk[] Chunks { get; set; }
	private ChunkData[] ChunkData { get; set; }

	private int numChunksX;
	private int numChunksY;
	private int numChunksZ;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Transform.OnTransformChanged += OnTransformChanged;

		RebuildChunks();
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();

		Transform.OnTransformChanged -= OnTransformChanged;

		DestroyChunks();
	}

	private void OnTransformChanged()
	{
		if ( Chunks == null )
			return;

		foreach ( var chunk in Chunks )
		{
			chunk.SetTransform( Transform.World );
		}
	}

	private void RebuildChunks()
	{
		DestroyChunks();

		numChunksX = SizeX / Chunk.ChunkSize;
		numChunksY = SizeY / Chunk.ChunkSize;
		numChunksZ = SizeZ / Chunk.ChunkSize;

		var chunkCount = numChunksX * numChunksY * numChunksZ;
		if ( chunkCount <= 0 )
			return;

		ChunkData = new ChunkData[chunkCount];
		Chunks = new Chunk[chunkCount];

		for ( int x = 0; x < numChunksX; ++x )
		{
			for ( int y = 0; y < numChunksY; ++y )
			{
				for ( int z = 0; z < numChunksZ; ++z )
				{
					var chunkIndex = x + y * numChunksX + z * numChunksX * numChunksY;
					var chunkData = new ChunkData( new Vector3Int( x * Chunk.ChunkSize, y * Chunk.ChunkSize, z * Chunk.ChunkSize ) );
					ChunkData[chunkIndex] = chunkData;
					Chunks[chunkIndex] = new Chunk( this, chunkData );
				}
			}
		}

		foreach ( var chunk in Chunks )
		{
			chunk.Rebuild();
			chunk.SetTransform( Transform.World );
		}

		SetBlockAndUpdate( new Vector3Int( 0, 0, 0 ), 1 );
		SetBlockAndUpdate( new Vector3Int( 1, 0, 0 ), 2 );
		SetBlockAndUpdate( new Vector3Int( 0, 1, 0 ), 3 );
		SetBlockAndUpdate( new Vector3Int( 0, 2, 0 ), 3 );
		SetBlockAndUpdate( new Vector3Int( 0, 3, 0 ), 3 );
	}

	private void DestroyChunks()
	{
		if ( Chunks != null )
		{
			foreach ( var chunk in Chunks )
			{
				if ( chunk == null )
					continue;

				chunk.Destroy();
			}
		}

		ChunkData = null;
		Chunks = null;
	}

	protected override void OnDirty()
	{
		base.OnDirty();

		RebuildChunks();
	}

	public bool SetBlockAndUpdate( Vector3Int blockPos, byte blocktype, bool forceUpdate = false )
	{
		bool build = false;
		var chunkids = new HashSet<int>();

		if ( SetBlock( blockPos, blocktype ) || forceUpdate )
		{
			var chunkIndex = GetBlockChunkIndexAtPosition( blockPos );

			chunkids.Add( chunkIndex );

			build = true;

			for ( int i = 0; i < 6; i++ )
			{
				if ( IsAdjacentBlockEmpty( blockPos, i ) )
				{
					var posInChunk = GetBlockPositionInChunk( blockPos );
					Chunks[chunkIndex].UpdateSlice( posInChunk, i );

					continue;
				}

				var adjacentPos = GetAdjacentBlockPosition( blockPos, i );
				var adjadentChunkIndex = GetBlockChunkIndexAtPosition( adjacentPos );
				var adjacentPosInChunk = GetBlockPositionInChunk( adjacentPos );

				chunkids.Add( adjadentChunkIndex );

				Chunks[adjadentChunkIndex].UpdateSlice( adjacentPosInChunk, GetOppositeDirection( i ) );
			}
		}

		foreach ( var chunkid in chunkids )
		{
			Chunks[chunkid].Build();
		}

		return build;
	}

	public int GetBlockChunkIndexAtPosition( Vector3Int pos )
	{
		return (pos.x / Chunk.ChunkSize) + (pos.y / Chunk.ChunkSize) * numChunksX + (pos.z / Chunk.ChunkSize) * numChunksX * numChunksY;
	}

	public static Vector3Int GetBlockPositionInChunk( Vector3Int pos )
	{
		return new Vector3Int( pos.x % Chunk.ChunkSize, pos.y % Chunk.ChunkSize, pos.z % Chunk.ChunkSize );
	}

	public static int GetOppositeDirection( int direction ) { return direction + ((direction % 2 != 0) ? -1 : 1); }

	public void SetBlockTypeAtPosition( Vector3Int pos, byte blockType )
	{
		if ( ChunkData == null )
			return;

		var chunkIndex = GetBlockChunkIndexAtPosition( pos );
		var blockPositionInChunk = GetBlockPositionInChunk( pos );
		var chunk = ChunkData[chunkIndex];

		chunk.SetBlockTypeAtPosition( blockPositionInChunk, blockType );
	}

	public byte GetBlockTypeAtPosition( Vector3Int pos )
	{
		var chunkIndex = GetBlockChunkIndexAtPosition( pos );
		var blockPositionInChunk = GetBlockPositionInChunk( pos );
		var chunk = Chunks[chunkIndex];

		return chunk.GetBlockTypeAtPosition( blockPositionInChunk );
	}

	public bool SetBlock( Vector3Int pos, byte blockType )
	{
		if ( pos.x < 0 || pos.x >= SizeX ) return false;
		if ( pos.y < 0 || pos.y >= SizeY ) return false;
		if ( pos.z < 0 || pos.z >= SizeZ ) return false;

		var chunkIndex = GetBlockChunkIndexAtPosition( pos );
		var blockPositionInChunk = GetBlockPositionInChunk( pos );
		int blockindex = Chunk.GetBlockIndexAtPosition( blockPositionInChunk );
		var chunk = Chunks[chunkIndex];
		int currentBlockType = chunk.GetBlockTypeAtIndex( blockindex );

		if ( blockType == currentBlockType )
		{
			return false;
		}

		if ( (blockType != 0 && currentBlockType == 0) || (blockType == 0 && currentBlockType != 0) )
		{
			chunk.SetBlockTypeAtIndex( blockindex, blockType );

			return true;
		}

		return false;
	}

	public static Vector3Int GetAdjacentBlockPosition( Vector3Int pos, int side )
	{
		return pos + Chunk.BlockDirections[side];
	}

	public bool IsAdjacentBlockEmpty( Vector3Int pos, int side )
	{
		return IsBlockEmpty( GetAdjacentBlockPosition( pos, side ) );
	}

	public bool IsBlockEmpty( Vector3Int pos )
	{
		if ( pos.x < 0 || pos.x >= SizeX ||
			 pos.y < 0 || pos.y >= SizeY )
		{
			return true;
		}

		if ( pos.z < 0 || pos.z >= SizeZ )
		{
			return true;
		}

		if ( pos.z >= SizeZ )
		{
			return true;
		}

		var chunkIndex = GetBlockChunkIndexAtPosition( pos );
		var blockPositionInChunk = GetBlockPositionInChunk( pos );
		var chunk = Chunks[chunkIndex];

		return chunk.GetBlockTypeAtPosition( blockPositionInChunk ) == 0;
	}

	public enum BlockFace : int
	{
		Invalid = -1,
		Top = 0,
		Bottom = 1,
		West = 2,
		East = 3,
		South = 4,
		North = 5,
	};

	public BlockFace GetBlockInDirection( Vector3 position, Vector3 direction, float length, out Vector3Int hitPosition, out float distance )
	{
		hitPosition = new Vector3Int( 0, 0, 0 );
		distance = 0;

		if ( direction.Length <= 0.0f )
		{
			return BlockFace.Invalid;
		}

		// distance from block position to edge of block
		var edgeOffset = new Vector3Int( direction.x < 0 ? 0 : 1,
							direction.y < 0 ? 0 : 1,
							direction.z < 0 ? 0 : 1 );

		// amount to step in each direction
		var stepAmount = new Vector3Int( direction.x < 0 ? -1 : 1,
							direction.y < 0 ? -1 : 1,
							direction.z < 0 ? -1 : 1 );

		// face that will be hit in each direction
		var faceDirection = new Vector3Int( direction.x < 0 ? (int)BlockFace.North : (int)BlockFace.South,
							   direction.y < 0 ? (int)BlockFace.East : (int)BlockFace.West,
							   direction.z < 0 ? (int)BlockFace.Top : (int)BlockFace.Bottom );

		var position3f = position; // start position
		distance = 0; // distance from starting position
		var ray = new Ray( position, direction );

		while ( true )
		{
			var position3i = new Vector3Int( (int)position3f.x, (int)position3f.y, (int)position3f.z ); // position of the block we are in

			// distance from current position to edge of block we are in
			var distanceToNearestEdge = new Vector3( position3i.x - position3f.x + edgeOffset.x,
											   position3i.y - position3f.y + edgeOffset.y,
											   position3i.z - position3f.z + edgeOffset.z );

			// if we are touching an edge, we are 1 unit away from the next edge
			for ( int i = 0; i < 3; ++i )
			{
				if ( MathF.Abs( distanceToNearestEdge[i] ) == 0.0f )
				{
					distanceToNearestEdge[i] = stepAmount[i];
				}
			}

			// length we must travel along the vector to reach the nearest edge in each direction
			var lengthToNearestEdge = new Vector3( MathF.Abs( distanceToNearestEdge.x / direction.x ),
											 MathF.Abs( distanceToNearestEdge.y / direction.y ),
											 MathF.Abs( distanceToNearestEdge.z / direction.z ) );

			int axis;

			// if the nearest edge in the x direction is the closest
			if ( lengthToNearestEdge.x < lengthToNearestEdge.y && lengthToNearestEdge.x < lengthToNearestEdge.z )
			{
				axis = 0;
			}
			// if the nearest edge in the y direction is the closest
			else if ( lengthToNearestEdge.y < lengthToNearestEdge.x && lengthToNearestEdge.y < lengthToNearestEdge.z )
			{
				axis = 1;
			}
			// if nearest edge in the z direction is the closest
			else
			{
				axis = 2;
			}

			distance += lengthToNearestEdge[axis];
			position3f = position + direction * distance;
			position3f[axis] = MathF.Floor( position3f[axis] + 0.5f * stepAmount[axis] );

			if ( position3f.x < 0.0f || position3f.y < 0.0f || position3f.z < 0.0f ||
				 position3f.x >= SizeX || position3f.y >= SizeY || position3f.z >= SizeZ )
			{
				break;
			}

			// last face hit
			var lastFace = (BlockFace)faceDirection[axis];

			// if we reached the length cap, exit
			if ( distance > length )
			{
				// made it all the way there
				distance = length;

				return BlockFace.Invalid;
			}

			// if there is a block at the current position, we have an intersection
			position3i = new Vector3Int( (int)position3f.x, (int)position3f.y, (int)position3f.z );

			var blockType = GetBlockTypeAtPosition( position3i );

			if ( blockType != 0 )
			{
				hitPosition = position3i;

				return lastFace;
			}
		}

		var plane = new Plane( new Vector3( 0.0f, 0.0f, 0.0f ), new Vector3( 0.0f, 0.0f, 1.0f ) );
		var distanceHit = 0.0f;
		var traceHitPos = plane.Trace( ray, true );
		if ( traceHitPos.HasValue )
		{
			distanceHit = Vector3.DistanceBetween( position, traceHitPos.Value );
		}

		if ( distanceHit >= 0.0f && distanceHit <= length )
		{
			var hitPosition3f = position + direction * distanceHit;

			if ( hitPosition3f.x < 0.0f || hitPosition3f.y < 0.0f || hitPosition3f.z < 0.0f ||
				 hitPosition3f.x > SizeX || hitPosition3f.y > SizeY || hitPosition3f.z > SizeZ )
			{
				// made it all the way there
				distance = length;

				return BlockFace.Invalid;
			}

			hitPosition3f.z = 0.0f;
			var blockHitPosition = new Vector3Int( (int)hitPosition3f.x, (int)hitPosition3f.y, (int)hitPosition3f.z );

			var blockType = GetBlockTypeAtPosition( blockHitPosition );

			if ( blockType == 0 )
			{
				distance = distanceHit;
				hitPosition = blockHitPosition;
				hitPosition.z = -1;

				return BlockFace.Top;
			}
		}

		// made it all the way there
		distance = length;

		return BlockFace.Invalid;
	}
}
