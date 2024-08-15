using System.Text.Json.Serialization;
using HC2;
using Sandbox.Diagnostics;

namespace Voxel;

[GameResource( "Block", "block", "Block", Icon = "square" )]
public class Block : GameResource, IValid
{
	/// <summary>
	/// The name of this block.
	/// </summary>
	[KeyProperty] public string Name { get; set; } = "Block";
	
	/// <summary>
	/// The color of this block.
	/// </summary>
	public Color Color { get; set; }

	/// <summary>
	/// How many of this block type can stack together.
	/// </summary>
	public int MaxStack { get; set; } = 99;

	/// <summary>
	/// The texture of this block.
	/// </summary>
	[Group( "Texture" )]
	public Texture Texture { get; set; }

	/// <summary>
	/// The texture size of this block.
	/// </summary>
	[Group( "Texture" )]
	public Vector2 TextureSize { get; set; } = 32f;

	/// <summary>
	/// What tags this block has.
	/// </summary>
	public TagSet Tags { get; set; } = new();
	
	/// <summary>
	/// Can the player break this block?
	/// </summary>
	public bool IsBreakable { get; set; }

	/// <summary>
	/// The max health of this block (where 0 is indestructible).
	/// </summary>
	[ShowIf( nameof( IsBreakable ), true ), Range( 0, 16 )] public int MaxHealth { get; set; } = 8;
	
	/// <summary>
	/// What kind of material this block is with regards to taking damage.
	/// </summary>
	[ShowIf( nameof( IsBreakable ), true )]
	public GatherSourceKind MaterialKind { get; set; }

	/// <summary>
	/// Scale damage to this block by this amount.
	/// </summary>
	[ShowIf( nameof( IsBreakable ), true )]
	public float DamageScale { get; set; } = 1f;

	/// <summary>
	/// Whether this block is valid.
	/// </summary>
	[JsonIgnore, Hide] public bool IsValid => true;

	protected override void PostReload()
	{
		Palette.OnReload?.Invoke();
		base.PostReload();
	}

	public void BlockDestroyed( VoxelRenderer renderer, Vector3 worldPos )
	{
		Assert.False( renderer.IsProxy );

		WorldItem.CreateInstance( Item.Create( this ), worldPos );
	}
}
