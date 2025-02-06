using System.Collections;
using USharpLibs.Engine.Registry;

namespace USharpLibs.Engine.RegistryNew {
	public static class TempMasterRegistry {
		private static Dictionary<string, IRegistry> MasterRegistry { get; } = new();

		// public static Registry<Item> ItemRegistry { get; } = RegisterRegistry<Item>("test_source");

		static TempMasterRegistry() { }

		public static Registry<T> RegisterRegistry<T>() where T : RegistryObject<T> {
			// TODO check name

			string regName = typeof(T).Name;

			if (!MasterRegistry.TryGetValue(regName, out IRegistry? reg)) {
				reg = new Registry<T>();
				MasterRegistry[regName] = reg;
			}

			return (Registry<T>)reg;
		}
	}

	public interface IRegistry { }
	public interface IRegistry<out T> : IRegistry, IEnumerable<T> where T : RegistryObject<T> { }

	public sealed class Registry<T> : IRegistry<T> where T : RegistryObject<T> {
		private Dictionary<AssetIdentifier, T> RegistryObjects { get; } = new();

		public T RegisterObject(string source, string name, T registryObject) {
			// TODO check name

			registryObject.RegistryKey = new(source, name);
			RegistryObjects[registryObject.RegistryKey] = registryObject;
			return registryObject;
		}

		public IEnumerator<T> GetEnumerator() => RegistryObjects.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	//

	public interface IRegistryObject { }

	public interface IRegistryObject<T> where T : IRegistryObject<T> { }

	public abstract class RegistryObject { }

	public abstract class RegistryObject<TSelf> : RegistryObject, IRegistryObject<TSelf> where TSelf : RegistryObject<TSelf> {
		public static Registry<TSelf> Registry { get; } = TempMasterRegistry.RegisterRegistry<TSelf>();

		public AssetIdentifier RegistryKey { get; internal set; } = null!;
	}

	//

	public class Item : RegistryObject<Item> {
		// public static Item TestItem = TempMasterRegistry.ItemRegistry.RegisterObject("source", "test_item", new ItemSuper());
		public static Item TestItem1 = Registry.RegisterObject("source", "test_item", new ItemSuper());
	}

	public class ItemSuper : Item { } // DOES THIS CREATE A NEW REGISTRY????
}