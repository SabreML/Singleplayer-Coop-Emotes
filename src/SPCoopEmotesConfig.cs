using Menu.Remix.MixedUI;
using UnityEngine;

namespace SingleplayerCoopEmotes
{
	public class SPCoopEmotesConfig : OptionInterface
	{
		public static Configurable<KeyCode> PointInput;

		public SPCoopEmotesConfig()
		{
			PointInput = config.Bind("PointInput", KeyCode.Space, new ConfigurableInfo("Input a button to change the pointing keybind. (Note: The map key requires a double-tap and hold)", tags: new object[]
			{
				"Point button keybind"
			}));
		}

		// Pretty much entirely taken from the music announcements config menu.
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
			AddKeyBinder();
		}

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

		private void AddKeyBinder()
		{
			OpKeyBinder keyBinder = new OpKeyBinder(PointInput, new Vector2(240, 475f), new Vector2(120f, 50f), false)
			{
				description = PointInput.info.description
			};

			OpLabel keyBinderLabel = new OpLabel(new Vector2(150f, 450f), new Vector2(300f, 30f), PointInput.info.Tags[0] as string);

			Tabs[0].AddItems(new UIelement[]
			{
				keyBinder,
				keyBinderLabel
			});
		}
	}
}
