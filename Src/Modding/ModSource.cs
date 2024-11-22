using System.Reflection;
using USharpLibs.Engine2.Utils;

namespace USharpLibs.Engine2.Modding {
	[PublicAPI]
	public class ModSource : IEquatable<ModSource> { // TODO expand upon this
		public string Source { get; }
		public ModVersion Version { get; }
		internal Assembly Assembly { get; }

		internal ModSource(Assembly assembly, ModVersion version) {
			Source = assembly.GetName().Name ?? throw new NullReferenceException("Unable to get assembly name.");
			Version = version;
			Assembly = assembly;
		}

		public bool Equals(ModSource? other) => other != null && Source == other.Source;
		public override bool Equals(object? obj) => obj is ModSource other && Equals(other);

		public override int GetHashCode() => Source.GetHashCode();

		public static bool operator ==(ModSource? left, ModSource? right) => Equals(left, right);
		public static bool operator !=(ModSource? left, ModSource? right) => !Equals(left, right);
	}
}