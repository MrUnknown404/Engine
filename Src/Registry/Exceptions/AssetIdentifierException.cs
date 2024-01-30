namespace USharpLibs.Engine.Registry.Exceptions {
	public class AssetIdentifierException : Exception {
		public AssetIdentifierException(Reason reason) : base(GenerateMessage(reason)) { }

		private static string GenerateMessage(Reason reason) =>
				reason switch {
						Reason.SourceEmpty => "Source cannot be null or empty.",
						Reason.SourceInvalid => $"Source does not follow the allowed Regex: {AssetIdentifier.Regex}",
						Reason.NameEmpty => "Name cannot be null or empty.",
						Reason.NameInvalid => $"Name does not follow the allowed Regex: {AssetIdentifier.Regex}",
						_ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
				};

		public enum Reason {
			SourceEmpty,
			SourceInvalid,
			NameEmpty,
			NameInvalid,
		}
	}
}