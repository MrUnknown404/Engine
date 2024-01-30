using JetBrains.Annotations;
using USharpLibs.Common.IO;
using USharpLibs.Engine.Registry.Exceptions;

namespace USharpLibs.Engine.Registry {
	[PublicAPI]
	public abstract class Registry {
		public string Source { get; }

		internal Registry(string source) {
			Source = source;

			// Normally i would call these first but #RegistryException uses the source/name so i can't
			if (!AssetIdentifier.NameRegex().IsMatch(source)) { throw new RegistryException(this, RegistryException.Reason.InvalidSource); }
		}
	}

	[PublicAPI]
	public abstract class Registry<T> : Registry where T : RegistryObject {
		private Dictionary<string, T> Collection { get; } = new();

		public IEnumerable<string> Keys => Collection.Keys;
		public IEnumerable<T> Values => Collection.Values;
		public bool IsEmpty => Collection.Count == 0;
		public int Count => Collection.Count;

		protected Registry(string source) : base(source) { }

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

			Logger.Debug($"Successfully registered object with id: '{item.Id}'");
		}

		public abstract void RegisterAll();
	}
}