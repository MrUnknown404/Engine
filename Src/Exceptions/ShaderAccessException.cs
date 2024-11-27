using USharpLibs.Engine2.Client.Shaders;

namespace USharpLibs.Engine2.Exceptions {
	[PublicAPI]
	public class ShaderAccessException : Exception {
		public Reason ReasonValue { get; }

		public ShaderAccessException(Reason reason) : base(ReasonToString(reason)) => ReasonValue = reason;

		private static string ReasonToString(Reason reason) =>
				reason switch {
						Reason.NoLongerValid => $"{nameof(ShaderAccess)} is no longer valid.",
						Reason.NeverRegistered => "Shader was never registered.",
						_ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
				};

		public enum Reason : byte {
			NoLongerValid = 0,
			NeverRegistered,
		}
	}
}