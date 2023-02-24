using Menu.Remix.MixedUI;
using UnityEngine;

namespace SingleplayerCoopEmotes
{
	public class SPCoopEmotesConfig : OptionInterface
	{
		public static Configurable<KeyCode> PointInput;

		// The instruction text explaining how to point in-game.
		private OpLabel pointingLabel;
		// The keybinder for the pointing emote button.
		private OpKeyBinder keyBinder;
		// (Both only used in `Update()`)

		public SPCoopEmotesConfig()
		{
			PointInput = config.Bind("PointInput", KeyCode.Space, new ConfigurableInfo("Input a button to change the pointing keybind. (Note: The map key requires a double-tap and hold)", tags: new object[]
			{
				"Point button keybind"
			}));
		}

		// This is all mostly just taken from the music announcements config menu.
		public override void Initialize()
		{
			base.Initialize();
			Tabs = new OpTab[]
			{
				new OpTab(this, "Options")
			};

			AddDivider(593f);
			AddTitle();
			AddDivider(540f);
			AddControlsText();
			AddKeyBinder();
		}

		// Updates the text for the pointing instructions based on the current value in `keyBinder`.
		// If the player is using the default map key then a double tap is required to start pointing, so this changes to reflect that.
		public override void Update()
		{
			string newText;

			// Default keybind.
			if (keyBinder.value == PointInput.defaultValue)
			{
				newText = "Double tap and hold the Point button with a movement input to start pointing in a direction.";
			}
			// Custom keybind.
			else
			{
				newText = "Press and hold the Point button with a movement input to start pointing in a direction.";
			}

			// Only go through the effort of updating it if the text has actually changed.
			if (pointingLabel.text != newText)
			{
				pointingLabel.text = newText;
			}
		}

		// Combines two flipped 'LinearGradient200's together to make a fancy looking divider.
		private void AddDivider(float y)
		{
			OpImage dividerLeft = new OpImage(new Vector2(300f, y), "LinearGradient200");
			dividerLeft.sprite.SetAnchor(0.5f, 0f);
			dividerLeft.sprite.rotation = 270f;

			OpImage dividerRight = new OpImage(new Vector2(300f, y), "LinearGradient200");
			dividerRight.sprite.SetAnchor(0.5f, 0f);
			dividerRight.sprite.rotation = 90f;

			Tabs[0].AddItems(new UIelement[]
			{
				dividerLeft,
				dividerRight
			});
		}

		// Adds the mod name and version to the interface between two dividers.
		private void AddTitle()
		{
			OpLabel title = new OpLabel(new Vector2(150f, 560f), new Vector2(300f, 30f), "Singleplayer Co-op Emotes", bigText: true);
			OpLabel version = new OpLabel(new Vector2(150f, 540f), new Vector2(300f, 30f), $"Version {SingleplayerCoopEmotes.Version}");

			Tabs[0].AddItems(new UIelement[]
			{
				title,
				version
			});
		}

		// Adds control instructions for each emote.
		private void AddControlsText()
		{
			OpLabel titleLabel = new OpLabel(new Vector2(150f, 480f), new Vector2(300f, 30f), "Controls:", bigText: true);

			// The text for this is added in `Update()`.
			pointingLabel = new OpLabel(new Vector2(150f, titleLabel.pos.y - 25f), new Vector2(300f, 30f));

			OpLabel sleepingLabel = new OpLabel(new Vector2(150f, titleLabel.pos.y - 45f), new Vector2(300f, 30f), "Hold Down while crawling to curl up into a ball and sleep.");

			Tabs[0].AddItems(new UIelement[]
			{
				titleLabel,
				pointingLabel,
				sleepingLabel
			});
		}

		// Adds a keybinder (and label) to the interface so that the pointing emote button can be rebound.
		private void AddKeyBinder()
		{
			keyBinder = new OpKeyBinder(PointInput, new Vector2(240f, 360f), new Vector2(120f, 50f), false)
			{
				description = PointInput.info.description
			};

			OpLabel keyBinderLabel = new OpLabel(new Vector2(150f, keyBinder.pos.y - 25f), new Vector2(300f, 30f), PointInput.info.Tags[0] as string);

			Tabs[0].AddItems(new UIelement[]
			{
				keyBinder,
				keyBinderLabel
			});
		}
	}
}
