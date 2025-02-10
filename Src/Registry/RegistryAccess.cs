using USharpLibs.Engine2.Modding;

namespace USharpLibs.Engine2.Registry {
	// TODO impl
	public class RegistryAccess<T> where T : RegistryObject {
		private Registry<T> Registry { get; }
		internal ModSource Source { get; set; } = null!; // throw, check

		internal RegistryAccess(Registry<T> registry) => Registry = registry;

		public V Register<V>(string key, V obj) where V : T => Registry.Register(Source, key, obj); // check
	}
}