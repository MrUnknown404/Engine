using System.Reflection;
using JetBrains.Annotations;
using USharpLibs.Common.IO;
using USharpLibs.Engine.Registry.Exceptions;

namespace USharpLibs.Engine.Registry {
	[PublicAPI]
	public static class RegistryH {
		private static Dictionary<RegistryKey, Registry> Registries { get; } = new();
		private static Dictionary<string, List<RegistryObject>> MasterRegistry { get; } = new();

		internal static void RegisterEverything() {
			foreach ((RegistryKey key, Registry registry) in Registries) {
				if (!MasterRegistry.TryGetValue(key.RegistryType, out List<RegistryObject>? list)) { MasterRegistry[key.RegistryType] = list = new(); }

				registry.RegisterAll();

				foreach (RegistryObject item in list) {
					// Should be safe to just add without fearing duplicates
					MasterRegistry[key.RegistryType].Add(item);
				}
			}
		}

		// TODO make a public version that mods can use
		internal static void RegisterRegistry(string source, Assembly assembly, Registry registry) {
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.SetupRegistries) { throw new($"Cannot register registries during {GameEngine.CurrentLoadState}"); }

			registry.Source = source;
			registry.SourceAssembly = assembly;

			// Normally i would call these first but #RegistryException uses the source/name so i can't
			if (!AssetIdentifier.NameRegex().IsMatch(source)) { throw new RegistryException(registry, RegistryException.Reason.InvalidSource); }

			if (Registries.Keys.Any(key => key.RegistryType == registry.RegistryType && key.RegistrySource == registry.Source)) { throw new RegistryException(registry, RegistryException.Reason.DuplicateSource); }

			Registries[new(registry.RegistryType, registry.Source)] = registry;
			Logger.Debug($"Successfully registered registry {registry}");
		}

		public static Registry<T>? GetRegistry<T>(string source) where T : RegistryObject {
			foreach ((RegistryKey key, Registry registry) in Registries) {
				if (key.RegistryType == typeof(T).Name && key.RegistrySource == source) { return (Registry<T>)registry; }
			}

			Logger.Error($"Could not find registry of type '{typeof(T).Name}' source '{source}'");
			return null;
		}
	}

	public class RegistryKey : IEquatable<RegistryKey> {
		public string RegistryType { get; }
		public string RegistrySource { get; }

		public RegistryKey(string registryType, string registrySource) {
			RegistryType = registryType;
			RegistrySource = registrySource;
		}

		public bool Equals(RegistryKey? other) => other != null && RegistrySource == other.RegistrySource && RegistryType == other.RegistryType;

		public override bool Equals(object? obj) {
			if (ReferenceEquals(null, obj) || !ReferenceEquals(this, obj)) { return false; }
			return obj.GetType() == GetType() && Equals((RegistryKey)obj);
		}

		public override int GetHashCode() => HashCode.Combine(RegistryType, RegistrySource);

		public static bool operator ==(RegistryKey? left, RegistryKey? right) => Equals(left, right);
		public static bool operator !=(RegistryKey? left, RegistryKey? right) => !Equals(left, right);
	}
}