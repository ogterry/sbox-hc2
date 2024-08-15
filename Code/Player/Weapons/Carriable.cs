using System;
using Sandbox.Citizen;

namespace HC2;

/// <summary>
/// Something we can carry in our hands
/// </summary>
public partial class Carriable : Component
{
	[Property, Group( "Carriable" )] public SupportedHand Hand { get; set; } = SupportedHand.MainHand;


	/// <summary>
	/// How do we hold this weapon?
	/// </summary>
	[Property, Group( "Visuals" )] public CitizenAnimationHelper.HoldTypes HoldType { get; set; } = CitizenAnimationHelper.HoldTypes.None;


	/// <summary>
	/// Tells us which hand we can put this item in (sometimes it can be both!)
	/// </summary>
	[Flags]
	public enum SupportedHand
	{
		[Title( "Main Hand" ), Icon( "back_hand" )]
		MainHand = 1 << 0,
		[Title( "Off Hand" ), Icon( "front_hand" )]
		OffHand = 1 << 1
	}
}
