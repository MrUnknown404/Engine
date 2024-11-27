using System.Text.RegularExpressions;
using USharpLibs.Engine2.Modding;

namespace USharpLibs.Engine2.Registry {
	[PublicAPI]
	public sealed partial class RegistryObjectIdentifier : IEquatable<RegistryObjectIdentifier> {
		public const string Regex = "^[a-z_]*$";

		public ModSource Source { get; }
		public string Key { get; }

		public RegistryObjectIdentifier(ModSource source, string key) {
			if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("Key cannot be null or empty."); }
			if (!NameRegex().IsMatch(key)) { throw new ArgumentException($"Key does not follow the allowed Regex: {Regex}"); }

			Source = source;
			Key = key;
		}

		[GeneratedRegex(Regex)] public static partial Regex NameRegex();

		public bool Equals(RegistryObjectIdentifier? other) => other != null && Source == other.Source && Key == other.Key;
		public override bool Equals(object? obj) => obj is RegistryObjectIdentifier other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(Source, Key);

		public static bool operator ==(RegistryObjectIdentifier? left, RegistryObjectIdentifier? right) => Equals(left, right);
		public static bool operator !=(RegistryObjectIdentifier? left, RegistryObjectIdentifier? right) => !Equals(left, right);
	}
}