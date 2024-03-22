namespace SingleplayerCoopEmotes
{
	// This is probably a really weird way of doing this, and there might already be a better solution somewhere,
	// but it works and I unfortunately don't have enough free time at the moment to come up something else.
	public readonly struct Error
	{
		public static Error None { get => new(Type.None); }
		public static Error UnknownError { get => new(Type.UnknownError); }
		public static Error CoopEnabled { get => new(Type.CoopEnabled); }

		public readonly Type ErrorType { get; }
		public readonly bool Exists { get => ErrorType != Type.None; }

		private Error(Type errorType)
		{
			ErrorType = errorType;
		}


		public enum Type
		{
			None,
			UnknownError,
			CoopEnabled,
		}
	}
}
