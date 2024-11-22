using JetBrains.Annotations;
using USharpLibs.Engine2.Modding;

namespace USharpLibs.Engine2.Registry {
	[PublicAPI]
	public abstract class RegistryObject<TSelf> where TSelf : RegistryObject<TSelf>, new() {
		private static Registry<TSelf> Registry { get; } = new();

		public RegistryObjectIdentifier Identifier { get; internal init; } = null!; // Set in Registry#Register

		protected static TObject Register<TObject>(ModSource source, string key) where TObject : TSelf, new() => (TObject)Registry.Register(source, key);
	}
}