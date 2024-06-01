using System.Collections;
using System.Reflection;
using JetBrains.Annotations;
using USharpLibs.Common.IO;
using USharpLibs.Engine.Registry.Exceptions;

namespace USharpLibs.Engine.Registry {
	[PublicAPI]
	public abstract class Registry {
		public string Source { get; internal set; } = default!; // Set in GameEngine
		public Assembly SourceAssembly { get; internal set; } = default!; // Set in GameEngine // TODO internal this when TextureAtlas is merged
		public abstract string RegistryType { get; }

		public abstract void RegisterAll();

		public override string ToString() => $"({RegistryType}:{Source})";
	}

	[PublicAPI]
	public abstract class Registry<T> : Registry, IEnumerable<T> where T : RegistryObject {
		private Dictionary<string, T> Collection { get; } = new();

		public override string RegistryType => typeof(T).Name;
		public IEnumerable<string> Keys => Collection.Keys;
		public IEnumerable<T> Values => Collection.Values;
		public bool IsEmpty => Collection.Count == 0;
		public int Count => Collection.Count;

		/// <param name="key"> Must follow the following Regex: ^[a-z_]*$</param>
		/// <param name="item"> The object to register </param>
		protected void Register(string key, T item) {
			item.Id = new(Source, key);

			// Normally i would call these first but #RegistryObjectException uses the source/name so i can't
			if (!AssetIdentifier.NameRegex().IsMatch(key)) {
				throw new RegistryObjectException(item, RegistryObjectException.Reason.InvalidKey); // technically i don't need this. AssetIdentifier#.ctor already does this
			} else if (Collection.ContainsKey(key)) {
				throw new RegistryObjectException(item, RegistryObjectException.Reason.DuplicateKey); // rider dumb
			}

			Collection[key] = item;

			Logger.Debug($"- Successfully registered object with id: '{item.Id}'");
		}

		public IEnumerator<T> GetEnumerator() => Collection.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}