﻿using BepInEx;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace SingleplayerCoopEmotes
{
	[BepInPlugin("sabreml.singleplayercoopemotes", "SingleplayerCoopEmotes", "1.3.1")]
	public class SingleplayerCoopEmotes : BaseUnityPlugin
	{
		/// <summary>
		/// The current mod version.
		/// </summary>
		public static string Version;

		/// <summary>
		/// Bool indicating if the 'Aim Anywhere' mod is enabled.
		/// </summary>
		private static bool aimAnywhereEnabled = false;


		public void OnEnable()
		{
			// Take the version number that was given to `BepInPlugin()` above.
			Version = Info.Metadata.Version.ToString();

			On.RainWorld.OnModsInit += Init;
			On.RainWorld.PostModsInit += PostInit;
		}


		private void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);

			string errorText = null;
			if (self.dlcVersion < 1)
			{
				errorText = "DLC not detected";
			}
			else if (ModManager.JollyCoop)
			{
				errorText = "Jolly Co-op is enabled";
			}

			// Only go beyond here if the DLC is installed and Jolly Co-op isn't currently loaded. (No reason to change anything otherwise)
			if (errorText != null)
			{
				Debug.Log($"(SPCoopEmotes) Error: {errorText}. Skipping hooks!");
				Logger.LogError(errorText);
				return;
			}

			// Regular hooks.
			On.Player.JollyUpdate += JollyUpdateHK;
			On.Player.JollyPointUpdate += JollyPointUpdateHK;
			On.PlayerGraphics.PlayerBlink += PlayerBlinkHK;

			// IL hooks to remove all `ModManager.CoopAvailable` checks for emotes.
			IL.Player.checkInput += RemoveCoopAvailableChecks;
			IL.Player.GraphicsModuleUpdated += RemoveCoopAvailableChecks;

			// And a manual one for the `Player.RevealMap` property getter.
			new ILHook(
				typeof(Player).GetProperty("RevealMap", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(),
				new ILContext.Manipulator(RemoveCoopAvailableChecks)
			);

			// Set up the remix menu.
			MachineConnector.SetRegisteredOI(Info.Metadata.GUID, new SPCoopEmotesConfig());
		}


		private void PostInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
		{
			orig(self);
			if (ModManager.ActiveMods.Any(mod => mod.id == "demo.aimanywhere"))
			{
				aimAnywhereEnabled = true;
			}
		}


		private void JollyUpdateHK(On.Player.orig_JollyUpdate orig, Player self, bool eu)
		{
			orig(self, eu);
			if (self.isNPC || self.room == null || self.DreamState)
			{
				return;
			}

			// Sleeping emote things.
			self.JollyEmoteUpdate();

			// Update the jolly button.
			UpdateJollyButton(self);

			// If the 'Aim Anywhere' mod is enabled, point in the direction of the mouse cursor.
			if (aimAnywhereEnabled)
			{
				AimAnywhereSupport.UpdatePointDirection(self);
			}

			// Pointing emote things.
			self.JollyPointUpdate();
		}


		// Updates `self.jollyButtonDown` based on the player's pointing keybind.
		// If the player isn't using a custom keybind, this copies the standard Jolly Co-op behaviour of a double-tap and hold.
		// If not, then this checks if the custom key is currently being held.
		private void UpdateJollyButton(Player self)
		{
			// The key which is set in the remix menu.
			KeyCode customKeybind = SPCoopEmotesConfig.PointKeybind.Value;

			// If the player is using a custom keybind.
			if (customKeybind != KeyCode.None)
			{
				self.jollyButtonDown = Input.GetKey(customKeybind);
				return;
			}

			// If the player isn't using a custom keybind, continue on to the standard double tap map key behaviour.
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
		}


		// Restores the (most likely unintentional) functionality from the 1.5 version of the mod,
		// of pointing with no movement input making your slugcat face towards the screen.
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
		// This override is the same as the original except with the Spearmaster check removed,
		// as it made them inconsistent with the other slugcats and it didn't seem like it was actually used for anything.
		// (This is for the sleeping animation)
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
		// (This has the same result as just removing the check.)
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
				// Set `label`'s target to `cursor.Next`, making the `brfalse` just jump to the next line.
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
