namespace USharpLibs.Engine.Registry.Exceptions {
	public class RegistryObjectException : Exception {
		public RegistryObjectException(RegistryObject registryObject, Reason reason) : base(GenerateMessage(registryObject, reason)) { }

		private static string GenerateMessage(RegistryObject registryObject, Reason reason) =>
				reason switch {
						Reason.InvalidKey => $"Key '{registryObject.Id.Name}' does not follow the allowed Regex: {AssetIdentifier.Regex}",
						Reason.DuplicateKey => $"Registry '{registryObject.Id.Source}' already contains key: '{registryObject.Id.Name}'",
						_ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
				};

		public enum Reason {
			InvalidKey,
			DuplicateKey,
		}
	}
}