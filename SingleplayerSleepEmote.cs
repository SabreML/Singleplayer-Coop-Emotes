using BepInEx;
using System.Security.Permissions;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace SingleplayerSleepEmote
{
	[BepInPlugin("sabreml.singleplayersleepemote", "SingleplayerSleepEmote", "1.0.0")]
	public class SingleplayerSleepEmote : BaseUnityPlugin
	{
		public void OnEnable()
		{
			On.Player.JollyUpdate += JollyUpdateHK;
		}

		private void JollyUpdateHK(On.Player.orig_JollyUpdate orig, Player self, bool eu)
		{
			orig(self, eu);

			// If the DLC is installed on Steam, and Jolly Co-op isn't currently loaded. (No point in this mod otherwise)
			if (RWCustom.Custom.rainWorld.dlcVersion > 0 && !ModManager.CoopAvailable)
			{
				self.JollyEmoteUpdate();
			}
		}
	}
}
