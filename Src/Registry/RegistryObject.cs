using JetBrains.Annotations;

namespace USharpLibs.Engine2.Registry {
	[PublicAPI]
	public abstract class RegistryObject {
		public RegistryObjectId Id { get; internal set; } = null!; // Set in Registry#Register
	}
}