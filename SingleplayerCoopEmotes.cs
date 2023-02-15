using BepInEx;
using System.Reflection;
using System.Security.Permissions;
using MonoMod.RuntimeDetour;
using System;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace SingleplayerCoopEmotes
{
	[BepInPlugin("sabreml.singleplayercoopemotes", "SingleplayerCoopEmotes", "1.1.2")]
	public class SingleplayerCoopEmotes : BaseUnityPlugin
	{
		public void OnEnable()
		{
			On.RainWorld.OnModsInit += Init;
		}

		private static void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			if (self.dlcVersion < 1)
			{
				Debug.Log("(SPCoopEmotes) Error: DLC not detected!");
				return;
			}
			if (ModManager.JollyCoop)
			{
				Debug.Log("(SPCoopEmotes) Error: Jolly Co-op is enabled!");
				return;
			}

			// Only load the hooks if the DLC is installed on Steam and Jolly Co-op isn't currently loaded. (No reason to change anything otherwise)

			// Regular hooks.
			On.Player.JollyUpdate += JollyUpdateHK;
			On.Player.JollyPointUpdate += JollyPointUpdateHK;
			On.Player.GraphicsModuleUpdated += GraphicsModuleUpdatedHK;
			On.Player.checkInput += checkInputHK;

			// Manual hook to override the `Player.RevealMap` property getter.
			new Hook(
				typeof(Player).GetProperty("RevealMap", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(),
				typeof(SingleplayerCoopEmotes).GetMethod(nameof(get_RevealMapHK), BindingFlags.NonPublic | BindingFlags.Static)
			);
		}


		private static void JollyUpdateHK(On.Player.orig_JollyUpdate orig, Player self, bool eu)
		{
			// If this is hooked then the checks above must have passed, so we don't need to worry about it trying to emote twice.
			orig(self, eu);
			if (self.isNPC || self.room == null)
			{
				return;
			}

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


		// Restores the (most likely unintentional) functionality from the 1.5 version of
		// pointing with no movement input making your slugcat face towards the screen.
		// (Technically, making them face towards the hand rendered behind their body.)
		//
		// Added by request :)
		private static void JollyPointUpdateHK(On.Player.orig_JollyPointUpdate orig, Player self)
		{
			orig(self);
			if (self.jollyButtonDown && self.PointDir() == Vector2.zero)
			{
				(self.graphicsModule as PlayerGraphics).LookAtPoint(self.mainBodyChunk.pos, 10f);
			}
		}


		// When Jolly Co-op is active and the jolly button is held, the `GraphicsModuleUpdated()` method makes held spears
		// point in the direction indicated by the player.
		private static void GraphicsModuleUpdatedHK(On.Player.orig_GraphicsModuleUpdated orig, Player self, bool actuallyViewed, bool eu)
		{
			orig(self, actuallyViewed, eu);

			// Recreation of the checks that need to pass in the base method to point a spear, but with the `ModManager.CoopAvailable` check removed.
			for (int i = 0; i < self.grasps.Length; i++)
			{
				if (self.grasps[i] == null || !actuallyViewed || !self.jollyButtonDown || self.handPointing != i)
				{
					return;
				}
				if (!(self.grasps[i].grabbed is Spear) || self.bodyMode == Player.BodyModeIndex.Crawl || self.animation == Player.AnimationIndex.ClimbOnBeam)
				{
					return;
				}
				Spear playerSpear = self.grasps[i].grabbed as Spear;

				playerSpear.setRotation = self.PointDir();
				playerSpear.rotationSpeed = 0f;
			}
		}


		// When Jolly Co-op is active and the jolly button is held, the `checkInput()` method skips opening the map
		// and sets the player's movement input as the pointing direction.
		private static void checkInputHK(On.Player.orig_checkInput orig, Player self)
		{
			// Temporarily make the method think that Jolly Co-op is loaded so that it checks for `jollyButtonDown`.
			if (!self.isNPC)
			{
				ModManager.CoopAvailable = true; // Don't do this for NPCs because it can break their AI.
			}
			orig(self);
			ModManager.CoopAvailable = false;
		}


		// Same as the original, except without a `ModManager.CoopAvailable` check.
		private static bool get_RevealMapHK(Func<Player, bool> orig, Player self)
		{
			return !self.jollyButtonDown && self.input[0].mp && !self.inVoidSea;
		}
	}
}
