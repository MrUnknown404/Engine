using System.Text.RegularExpressions;
using JetBrains.Annotations;
using USharpLibs.Engine2.Modding;

namespace USharpLibs.Engine2.Registry {
	[PublicAPI]
	public sealed partial class RegistryObjectId : IEquatable<RegistryObjectId> {
		public const string Regex = "^[a-z_]*$";

		public ModSource Source { get; }
		public string Key { get; }

		public RegistryObjectId(ModSource source, string key) {
			if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("Key cannot be null or empty."); } // TODO custom exception
			if (!NameRegex().IsMatch(key)) { throw new ArgumentException($"Key does not follow the allowed Regex: {Regex}"); }

			Source = source;
			Key = key;
		}

		[GeneratedRegex(Regex)] public static partial Regex NameRegex();

		public bool Equals(RegistryObjectId? other) => other != null && Source == other.Source && Key == other.Key;
		public override bool Equals(object? obj) => obj is RegistryObjectId other && Equals(other);
		public override int GetHashCode() => HashCode.Combine(Source, Key);
		public override string ToString() => $"{Source.Source}:{Key}";

		public static bool operator ==(RegistryObjectId? left, RegistryObjectId? right) => Equals(left, right);
		public static bool operator !=(RegistryObjectId? left, RegistryObjectId? right) => !Equals(left, right);
	}
}