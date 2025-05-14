using BepInEx;
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
	/// <summary>
	/// Enum of possible mod initialisation error types.
	/// </summary>
	public enum ErrorType
	{
		None,
		JollyCoopEnabled,
		ILEditFailed
	}

	[BepInPlugin("sabreml.singleplayercoopemotes", "SingleplayerCoopEmotes", VERSION)]
	public class SingleplayerCoopEmotes : BaseUnityPlugin
	{
		/// <summary>
		/// The current mod version.
		/// </summary>
		public const string VERSION = "1.4.6";

		/// <summary>
		/// The error state from the mod's initialisation.
		/// </summary>
		public static ErrorType InitError = ErrorType.None;

		/// <summary>
		/// Bool indicating if the 'Aim Anywhere' mod is enabled.
		/// </summary>
		private static bool aimAnywhereEnabled;


		public void OnEnable()
		{
			On.RainWorld.OnModsInit += Init;
			On.RainWorld.PostModsInit += PostInit;
		}

		private void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);

			// Before anything else, set up the remix menu.
			MachineConnector.SetRegisteredOI(Info.Metadata.GUID, new SPCoopEmotesConfig());

			// If Jolly Co-op is enabled, then this mod will conflict with it.
			if (ModManager.JollyCoop)
			{
				InitError = ErrorType.JollyCoopEnabled;
			}

			// Regular hooks.
			On.Player.JollyUpdate += JollyUpdateHK;
			On.Player.JollyPointUpdate += JollyPointUpdateHK;
			On.PlayerGraphics.PlayerBlink += PlayerBlinkHK;

			// IL hooks to remove all `ModManager.CoopAvailable` checks for emotes.
			IL.Player.checkInput += RemoveCoopAvailableChecks;
			IL.Player.GraphicsModuleUpdated += RemoveCoopAvailableChecks; // todo check if this is needed
			IL.Player.GetHeldItemDirection += RemoveCoopAvailableChecks;

			// And a manual one for the `Player.RevealMap` property getter.
			new ILHook(
				typeof(Player).GetProperty("RevealMap", BindingFlags.Public|BindingFlags.Instance).GetGetMethod(),
				new ILContext.Manipulator(RemoveCoopAvailableChecks)
			);
		}

		private void PostInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
		{
			orig(self);
			aimAnywhereEnabled = ModManager.ActiveMods.Any(mod => mod.id == "demo.aimanywhere");

			if (InitError == ErrorType.None)
			{
				Debug.Log("(SPCoopEmotes) Mod initialised successfully!");
			}
			// If any errors came up.
			else
			{
				string logMessage = $"(SPCoopEmotes) Error: Mod init failed with error type {{{InitError}}}!";
				Debug.Log(logMessage);
				Debug.LogException(new Exception(logMessage));
			}
		}


		private void JollyUpdateHK(On.Player.orig_JollyUpdate orig, Player self, bool eu)
		{
			orig(self, eu);
			if (InitError != ErrorType.None)
			{
				return;
			}

			if (self.isNPC || self.DreamState || self.room == null)
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
		// If they are using a custom keybind, then this checks if the custom key is currently being held.
		private void UpdateJollyButton(Player self)
		{
			// The key which has been set in the remix menu.
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
			if (InitError != ErrorType.None)
			{
				return;
			}

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
			if (InitError != ErrorType.None)
			{
				orig(self);
				return;
			}

			if (UnityEngine.Random.value < 0.033333335f)
			{
				self.blink = Math.Max(2, self.blink);
			}
			if (self.player.sleepCurlUp == 1f)
			{
				self.blink = Math.Max(2, self.blink);
			}
		}


		// This is used to go to each `ModManager.CoopAvailable` check in the method and set its `brfalse` target to the next instruction,
		// which has the same result as just removing the check.
		private void RemoveCoopAvailableChecks(ILContext il)
		{
			ILCursor cursor = new(il);
			bool editSuccessful = false;

			ILLabel label = null;
			// Match a `ldsfld` for `ModManager.CoopAvailable`, followed by a `brfalse`.
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
				InitError = ErrorType.ILEditFailed;
			}
		}
	}
}
