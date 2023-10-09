using JetBrains.Annotations;

namespace USharpLibs.Engine.Registry {
	[PublicAPI]
	public abstract class RegistryObject {
		public AssetLocation Id { get; internal set; } = default!;
	}
}