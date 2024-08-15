using System.Text.Json.Serialization;
using Sandbox;

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
	[Range( 0, 16)] public int MaxHealth { get; set; } = 8;
	
	/// <summary>
	/// Which tool tags are allowed to break this block.
	/// </summary>
	[ShowIf( nameof( IsBreakable ), true )]
	public TagSet AllowedTools { get; set; }

	/// <summary>
	/// The minimum effectiveness that a tool must have to break this block.
	/// </summary>
	[ShowIf( nameof( IsBreakable ), true )]
	[Range( 0f, 1f )] public float MinimumEffectiveness { get; set; } = 0f;

	/// <summary>
	/// Scale damage from tools by this amount.
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
}
