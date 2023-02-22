using BepInEx;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace SingleplayerCoopEmotes
{
	[BepInPlugin("sabreml.singleplayercoopemotes", "SingleplayerCoopEmotes", "1.1.6")]
	public class SingleplayerCoopEmotes : BaseUnityPlugin
	{
		public void OnEnable()
		{
			On.RainWorld.OnModsInit += Init;
		}

		private void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			if (self.dlcVersion < 1)
			{
				Debug.Log("(SPCoopEmotes) Error: DLC not detected!");
				Logger.LogError("DLC not detected!");
				return;
			}
			if (ModManager.JollyCoop)
			{
				Debug.Log("(SPCoopEmotes) Error: Jolly Co-op is enabled!");
				Logger.LogError("Jolly Co-op is enabled!");
				return;
			}

			// Only load the hooks if the DLC is installed on Steam and Jolly Co-op isn't currently loaded. (No reason to change anything otherwise)

			// Regular hooks.
			On.Player.JollyUpdate += JollyUpdateHK;
			On.Player.JollyPointUpdate += JollyPointUpdateHK;
			On.PlayerGraphics.PlayerBlink += PlayerBlinkHK;

			// IL hooks to remove all `ModManager.CoopAvailable` checks.
			IL.Player.checkInput += RemoveCoopAvailableChecks;
			IL.Player.GraphicsModuleUpdated += RemoveCoopAvailableChecks;

			// And a manual one for the `Player.RevealMap` property getter.
			new ILHook(
				typeof(Player).GetProperty("RevealMap", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(),
				new ILContext.Manipulator(RemoveCoopAvailableChecks)
			);
		}


		private void JollyUpdateHK(On.Player.orig_JollyUpdate orig, Player self, bool eu)
		{
			// If this is hooked then the checks above must have passed, so we don't need to worry about it trying to emote twice.
			orig(self, eu);
			if (self.isNPC || self.room == null || self.abstractCreature.world.game.wasAnArtificerDream)
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
		private void JollyPointUpdateHK(On.Player.orig_JollyPointUpdate orig, Player self)
		{
			orig(self);
			if (self.jollyButtonDown && self.PointDir() == Vector2.zero)
			{
				(self.graphicsModule as PlayerGraphics)?.LookAtPoint(self.mainBodyChunk.pos, 10f);
			}
		}


		// Called by `PlayerGraphics.Update()` when the player has fully curled up to sleep.
		//
		// This override is the same as the original except without the Spearmaster check, as it made them inconsistent with
		// the other slugcats, and it didn't seem like it was actually used for anything.
		private void PlayerBlinkHK(On.PlayerGraphics.orig_PlayerBlink orig, PlayerGraphics self)
		{
			if (UnityEngine.Random.value < 0.033333335f)
			{
				self.blink = Math.Max(2, self.blink);
			}
			if (self.player.sleepCurlUp == 1f)
			{
				self.blink = Math.Max(2, self.blink);
			}
		}


		// This is used to go to each `ModManager.CoopAvailable` check in the method and set its `brfalse` target to the next instruction.
		// (This has the same result as just removing the check, but for some reason I can't get that to work.)
		private void RemoveCoopAvailableChecks(ILContext il)
		{
			ILCursor cursor = new ILCursor(il);
			bool editSuccessful = false;

			ILLabel label = null;
			while (cursor.TryGotoNext(MoveType.After,
				i => i.MatchLdsfld<ModManager>("CoopAvailable"),
				i => i.MatchBrfalse(out label) // Assign the `ILLabel` from this instruction to the `label` variable.
			))
			{
				// Set `label`'s target to `cursor.Next`.
				cursor.MarkLabel(label);
				editSuccessful = true;
			}

			// If it wasn't able to find and edit at least one `CoopAvailable` check, then somthing went wrong.
			if (!editSuccessful)
			{
				Debug.Log("(SPCoopEmotes) Error: IL edit failed!");
				Logger.LogError("IL edit failed!");
			}
		}
	}
}
