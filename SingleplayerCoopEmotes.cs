using BepInEx;
using System.Reflection;
using System.Security.Permissions;
using MonoMod.RuntimeDetour;
using UnityEngine;
using System;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace SingleplayerCoopEmotes
{
	[BepInPlugin("sabreml.singleplayercoopemotes", "SingleplayerCoopEmotes", "1.1.0")]
	public class SingleplayerCoopEmotes : BaseUnityPlugin
	{
		public void OnEnable()
		{
			On.RainWorld.OnModsInit += Init;
		}

		private void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			// If the DLC is installed on Steam, and Jolly Co-op isn't currently loaded. (No reason to change anything otherwise)
			if (RWCustom.Custom.rainWorld.dlcVersion > 0 && !ModManager.JollyCoop)
			{
				On.Player.JollyUpdate += JollyUpdateHK;
				On.Player.checkInput += checkInputHK;

				// Manual hook to override the `Player.RevealMap` property getter.
				new Hook(
					typeof(Player).GetProperty("RevealMap", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(),
					typeof(SingleplayerCoopEmotes).GetMethod(nameof(get_RevealMapHK), BindingFlags.NonPublic | BindingFlags.Static)
				);
			}
		}

		private static void JollyUpdateHK(On.Player.orig_JollyUpdate orig, Player self, bool eu)
		{
			// If this is hooked then the check above must have passed, so we don't need to worry about conflicts.
			orig(self, eu);

			// Sleeping emote things.
			self.JollyEmoteUpdate();

			// Update the jolly button. (Taken from `JollyInputUpdate()`)
			if (!self.input[0].mp) // If the button isn't being held down at all.
			{
				self.jollyButtonDown = false;
			}
			else if (!self.input[1].mp) // If the button was down this frame, but not last frame.
			{
				self.jollyButtonDown = false;
				for (int i = 2; i < self.input.Length - 1; i++)
				{
					if (self.input[i].mp && !self.input[i + 1].mp) // Look for a double tap.
					{
						self.jollyButtonDown = true;
					}
				}
			}
			// Pointing emote things.
			self.JollyPointUpdate();
		}

		private static void checkInputHK(On.Player.orig_checkInput orig, Player self)
		{
			// Temporarily make it think that Jolly Co-op is loaded so that it checks for `jollyButtonDown`.
			// (Doing it this way is a lot easier than trying to edit the method.)
			ModManager.CoopAvailable = true;
			orig(self);
			ModManager.CoopAvailable = false;
		}

		// Same as the original getter except without a `ModManager.CoopAvailable` check.
		private static bool get_RevealMapHK(Func<Player, bool> orig, Player self)
		{
			return !self.jollyButtonDown && self.input[0].mp && !self.inVoidSea;
		}
	}
}
