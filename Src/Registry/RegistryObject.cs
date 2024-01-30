using JetBrains.Annotations;

namespace USharpLibs.Engine.Registry {
	[PublicAPI]
	public abstract class RegistryObject {
		public AssetIdentifier Id { get; internal set; } = default!;
	}
}