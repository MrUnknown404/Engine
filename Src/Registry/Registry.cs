using System.Collections;
using USharpLibs.Engine2.Modding;

namespace USharpLibs.Engine2.Registry {
	public sealed class Registry<T> : IEnumerable<T> where T : RegistryObject<T>, new() {
		private Dictionary<RegistryObjectIdentifier, T> AllObjects { get; } = new();
		private Dictionary<ModSource, HashSet<T>> ObjectsBySource { get; } = new();

		internal T Register(ModSource source, string key) {
			RegistryObjectIdentifier id = new(source, key);
			T obj = new() { Identifier = id, };

			AllObjects[id] = obj;

			if (!ObjectsBySource.TryGetValue(source, out HashSet<T>? set)) { ObjectsBySource[source] = set ??= new(); }
			set.Add(obj);

			return obj;
		}

		public T? GetById(RegistryObjectIdentifier id) => AllObjects.GetValueOrDefault(id);
		public IEnumerable<T> GetBySource(ModSource source) => ObjectsBySource.GetValueOrDefault(source, new());

		public IEnumerator<T> GetEnumerator() => AllObjects.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}