public partial class Player
{
	/// <summary>
	/// The player's main weapon, the one we're using right now
	/// </summary>
	[Property, Sync] 
	public WeaponComponent MainWeapon { get; set; }
}
