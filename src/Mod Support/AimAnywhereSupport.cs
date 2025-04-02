using UnityEngine;

namespace SingleplayerCoopEmotes
{
	public static class AimAnywhereSupport
	{
		// Mostly taken from Aim Anywhere's `WeaponPatch.WeaponThrownPatch()`.
		public static void UpdatePointDirection(Player self)
		{
			if (self.room.game.cameras[0] == null)
			{
				return;
			}

			Vector2 aimDirection = new(Input.mousePosition.x, Input.mousePosition.y);
			aimDirection += (self.room.game.cameras[0].pos - self.mainBodyChunk.pos);

			self.pointInput.analogueDir = aimDirection.normalized;
		}
	}
}
