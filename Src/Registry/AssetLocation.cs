using System.Text.RegularExpressions;

namespace USharpLibs.Engine.Registry {
	public partial class AssetLocation : IEquatable<AssetLocation> {
		public const string Regex = "^[a-z_]*$";

		public string Source { get; }
		public string Name { get; }

		public AssetLocation(string source, string name) {
			if (string.IsNullOrWhiteSpace(source)) { throw new ArgumentException("Source cannot be null or empty."); }
			if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("Name cannot be null or empty."); }
			if (!NameRegex().IsMatch(source)) { throw new ArgumentException($"Source does not follow the allowed Regex: {Regex}"); }
			if (!NameRegex().IsMatch(name)) { throw new ArgumentException($"Name does not follow the allowed Regex: {Regex}"); }

			Source = source;
			Name = name;
		}

		[GeneratedRegex(Regex)] public static partial Regex NameRegex();

		public bool Equals(AssetLocation? other) => other != null && Source == other.Source && Name == other.Name;

		public override bool Equals(object? obj) {
			if (ReferenceEquals(null, obj) || !ReferenceEquals(this, obj)) { return false; }
			return obj.GetType() == GetType() && Equals((AssetLocation)obj);
		}

		public override int GetHashCode() => HashCode.Combine(Source, Name);

		public static bool operator ==(AssetLocation? left, AssetLocation? right) => Equals(left, right);
		public static bool operator !=(AssetLocation? left, AssetLocation? right) => !Equals(left, right);

		public override string ToString() => $"{Source}:{Name}";
	}
}