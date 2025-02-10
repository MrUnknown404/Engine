using System.Collections;
using USharpLibs.Engine2.Modding;

namespace USharpLibs.Engine2.Registry {
	// TODO impl this
	// protected static Registry<T> CreateRegister<T>() where T : RegistryObject { // check
	// 	Registry<T> r = new();
	// 	// register
	// 	return r;
	// }

	// TODO maybe create a wrapper class for RegistryObjects? so we can load registries when we should and not on accident

	public sealed class Registry<T> : IEnumerable<T> where T : RegistryObject {
		private Dictionary<RegistryObjectId, T> AllObjects { get; } = new();
		private Dictionary<ModSource, HashSet<T>> ObjectsBySource { get; } = new();
		private RegistryAccess<T> RegistryAccess { get; } // make sure to clear once done

		internal Registry() => RegistryAccess = new(this);

		public RegistryAccess<T> GetAccess(ModSource source) {
			RegistryAccess.Source = source;
			return RegistryAccess;
		}

		internal V Register<V>(ModSource source, string key, V obj) where V : T {
			RegistryObjectId id = new(source, key);
			obj.Id = id;

			if (!ObjectsBySource.TryGetValue(source, out HashSet<T>? set)) {
				set = new();
				ObjectsBySource[source] = set;
			}

			AllObjects[id] = obj;
			set.Add(obj);

			return obj;
		}

		public T? GetById(RegistryObjectId id) => AllObjects.GetValueOrDefault(id);
		public IEnumerable<T> GetBySource(ModSource source) => ObjectsBySource.GetValueOrDefault(source, new());

		public IEnumerator<T> GetEnumerator() => AllObjects.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}