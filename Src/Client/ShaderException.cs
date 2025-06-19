using Engine3.Utils;

namespace Engine3.Client {
	public class ShaderException : Exception, IReasonEnumException<ShaderException.Reason> {
		public Reason ReasonRef { get; }

		public ShaderException(Reason reasonRef) : base(reasonRef switch {
				Reason.NoHandle => expr,
				Reason.HasHandle => expr,
				Reason.FailedToLink => expr,
				Reason.FailedToCompile => expr,
				Reason.NotBound => expr,
				_ => throw new ArgumentOutOfRangeException(nameof(reasonRef), reasonRef, null),
		}) => ReasonRef = reasonRef;

		public enum Reason : byte {
			NoHandle = 0,
			HasHandle,
			FailedToLink,
			FailedToCompile,
			NotBound,
		}
	}
}