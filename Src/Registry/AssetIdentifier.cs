using System.Text.RegularExpressions;
using USharpLibs.Engine.Registry.Exceptions;

namespace USharpLibs.Engine.Registry {
	public partial class AssetIdentifier : IEquatable<AssetIdentifier> {
		public const string Regex = "^[a-z_]*$";

		public string Source { get; }
		public string Name { get; }

		public AssetIdentifier(string source, string name) {
			if (string.IsNullOrWhiteSpace(source)) { throw new AssetIdentifierException(AssetIdentifierException.Reason.SourceEmpty); }
			if (string.IsNullOrWhiteSpace(name)) { throw new AssetIdentifierException(AssetIdentifierException.Reason.NameEmpty); }
			if (!NameRegex().IsMatch(source)) { throw new AssetIdentifierException(AssetIdentifierException.Reason.SourceInvalid); }
			if (!NameRegex().IsMatch(name)) { throw new AssetIdentifierException(AssetIdentifierException.Reason.NameInvalid); }

			Source = source;
			Name = name;
		}

		[GeneratedRegex(Regex)] public static partial Regex NameRegex();

		public bool Equals(AssetIdentifier? other) => other != null && Source == other.Source && Name == other.Name;

		public override bool Equals(object? obj) {
			if (ReferenceEquals(null, obj) || !ReferenceEquals(this, obj)) { return false; }
			return obj.GetType() == GetType() && Equals((AssetIdentifier)obj);
		}

		public override int GetHashCode() => HashCode.Combine(Source, Name);

		public static bool operator ==(AssetIdentifier? left, AssetIdentifier? right) => Equals(left, right);
		public static bool operator !=(AssetIdentifier? left, AssetIdentifier? right) => !Equals(left, right);

		public override string ToString() => $"{Source}:{Name}";
	}
}