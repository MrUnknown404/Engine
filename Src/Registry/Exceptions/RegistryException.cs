namespace USharpLibs.Engine.Registry.Exceptions {
	public class RegistryException : Exception {
		public RegistryException(Registry registry, Reason reason) : base(GenerateMessage(registry, reason)) { }

		private static string GenerateMessage(Registry registry, Reason reason) =>
				reason switch {
						Reason.InvalidSource => $"Source '{registry.Source}' does not follow the allowed Regex: {AssetIdentifier.Regex}",
						Reason.DuplicateSource => $"Cannot add registry of source '{registry.Source}' because it already exists!",
						_ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
				};

		public enum Reason {
			InvalidSource,
			DuplicateSource,
		}
	}
}