using JetBrains.Annotations;
using USharpLibs.Common.Utils;

namespace USharpLibs.Engine.Registry {
	[PublicAPI]
	public abstract class Registry<T> where T : RegistryObject {
		public string Source { get; }
		private Dictionary<string, T> Collection { get; } = new();

		public IEnumerable<string> Keys => Collection.Keys;
		public IEnumerable<T> Values => Collection.Values;
		public bool IsEmpty => Collection.Count == 0;
		public int Count => Collection.Count;

		protected Registry(string source) {
			if (!AssetLocation.NameRegex().IsMatch(source)) { throw new ArgumentException($"Source does not follow the allowed Regex: {AssetLocation.Regex}"); }
			Source = source;
		}

		/// <param name="key"> Must follow the following Regex: ^[a-z_]*$</param>
		/// <param name="item"> The object to register </param>
		protected void Register(string key, T item) {
			if (!AssetLocation.NameRegex().IsMatch(key)) {
				Logger.Error($"Key {key} does not follow the allowed Regex: {AssetLocation.Regex}");
				return;
			} else if (Collection.ContainsKey(key)) {
				Logger.Warn($"Registry {Source} already contains key: '{key}'");
				return;
			}

			item.Id = new(Source, key);
			Collection[key] = item;
		}

		public abstract void RegisterAll();
	}
}