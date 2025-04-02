using Menu.Remix.MixedUI;
using UnityEngine;

namespace SingleplayerCoopEmotes
{
	public class SPCoopEmotesConfig : OptionInterface
	{
		public static Configurable<KeyCode> PointKeybind;

		// The instruction text explaining how to point in-game.
		private OpLabel pointingLabel;
		// The keybinder for the pointing emote button.
		private ResettableKeyBinder keyBinder;
		// (Both only used in `Update()`)

		public SPCoopEmotesConfig()
		{
			PointKeybind = config.Bind("PointKeybind", KeyCode.None, new ConfigurableInfo("Input a button to change the pointing keybind. (Input the 'Escape' key to reset to default)", tags: new object[]
			{
				"Custom pointing keybind"
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

			if (SingleplayerCoopEmotes.InitError.Exists)
			{
				AddErrorMessage();
			}
		}

		// Updates the text for the pointing instructions based on the current value in `keyBinder`.
		// If the player is using the default map key then a double tap is required to start pointing, so this changes to reflect that.
		public override void Update()
		{
			string newLabelText;

			// Using the map button. (Default behaviour)
			if (keyBinder.value == OpKeyBinder.NONE)
			{
				newLabelText = "Double tap and hold the Map button with a movement input to start pointing in a direction.";
			}
			// Using a custom keybind.
			else
			{
				newLabelText = "Press and hold the Point button with a movement input to start pointing in a direction.";
			}

			// Only go through the effort of updating the `text` property if the text has actually changed.
			if (pointingLabel.text != newLabelText)
			{
				pointingLabel.text = newLabelText;
			}
		}

		// Combines two flipped 'LinearGradient200's together to make a fancy looking divider.
		private void AddDivider(float y)
		{
			OpImage dividerLeft = new(new Vector2(300f, y), "LinearGradient200");
			dividerLeft.sprite.SetAnchor(0.5f, 0f);
			dividerLeft.sprite.rotation = 270f;

			OpImage dividerRight = new(new Vector2(300f, y), "LinearGradient200");
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
			OpLabel title = new(new Vector2(150f, 560f), new Vector2(300f, 30f), "Singleplayer Co-op Emotes", bigText: true);
			OpLabel version = new(new Vector2(150f, 540f), new Vector2(300f, 30f), $"Version {SingleplayerCoopEmotes.VERSION}");

			Tabs[0].AddItems(new UIelement[]
			{
				title,
				version
			});
		}

		// Adds control instructions for each emote.
		private void AddControlsText()
		{
			OpLabel titleLabel = new(new Vector2(150f, 490f), new Vector2(300f, 30f), "Controls:", bigText: true);

			// The text for this is added in `Update()`.
			pointingLabel = new OpLabel(new Vector2(150f, titleLabel.pos.y - 25f), new Vector2(300f, 30f), "Error: Failed to update help text!");

			OpLabel sleepingLabel = new(new Vector2(150f, pointingLabel.pos.y - 20f), new Vector2(300f, 30f), "Hold Down while crawling to curl up into a ball and sleep!");

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
			keyBinder = new ResettableKeyBinder(PointKeybind, new Vector2(240f, 370f), new Vector2(120f, 50f), false)
			{
				description = PointKeybind.info.description
			};
			// Update the instructions text every time the keybind is changed.
			keyBinder.OnChange += Update;

			OpLabel keyBinderLabel = new(new Vector2(150f, keyBinder.pos.y - 25f), new Vector2(300f, 30f), PointKeybind.info.Tags[0] as string);
			OpLabel warningLabel = new(new Vector2(150f, keyBinderLabel.pos.y - 20f), new Vector2(300f, 30f),
				"(Make sure this doesn't conflict with other keybinds!)")
			{
				color = new Color(0.517f, 0.506f, 0.534f)
			};

			Tabs[0].AddItems(new UIelement[]
			{
				keyBinder,
				keyBinderLabel,
				warningLabel
			});
		}

		// Adds a boxed error message
		private void AddErrorMessage()
		{
			Color color = new(0.85f, 0.35f, 0.4f);
			Vector2 size = new(350f, 150f);
			Vector2 position = new(125f, 150f);

			// The red box around the error message.
			OpRect containerRect = new(position, size);
			containerRect.colorEdge = color;

			// lil' slug head
			Vector2 scugPos = position + (size / 2) + new Vector2(0f, 19f);
			OpImage scug = new(scugPos, "Multiplayer_Death");
			scug.pos -= scug._size / 2; // Centre the sprite
			scug.color = color;

			// The error message
			OpLabel textLabel = new(
				position,
				size,
				@$"
					.
					An error has occurred during mod initialisation!
					Error type: {{{SingleplayerCoopEmotes.InitError.ErrorType}}}
				", // The dot on the first line is there to shift the text down for the sprite, which covers it.
				FLabelAlignment.Center
			);
			textLabel.color = color;
			textLabel.autoWrap = true;
			textLabel.Change();

			Tabs[0].AddItems(new UIelement[]
			{
				containerRect,
				scug,
				textLabel
			});
		}
	}

	// OpKeyBinders mostly work like this already, but for some reason it can still bind to the escape key if the player is using a controller.
	// So this is just here to handle that edge case.
	public class ResettableKeyBinder : OpKeyBinder
	{
		public ResettableKeyBinder(Configurable<KeyCode> config, Vector2 pos, Vector2 size, bool collisionCheck = true,
			BindController controllerNo = BindController.AnyController) : base(config, pos, size, collisionCheck, controllerNo)
		{}

		public override string value
		{
			get => base.value;
			set
			{
				// If the key trying to be assigned is different from the current one.
				if (base.value != value)
				{
					// If that key is Esc, reset the keybinder to empty/`NONE`.
					if (value == "Escape")
					{
						value = OpKeyBinder.NONE;
					}
					// Otherwise just pass it to the standard `OpKeyBinder` system.
					base.value = value;
				}
			}
		}
	}
}
