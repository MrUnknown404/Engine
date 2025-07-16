using Engine3.Utils;

namespace Engine3.Client {
	public class VaoException : Exception, IReasonEnumException<VaoException.Reason> {
		public Reason ReasonRef { get; }

		public VaoException(Reason reasonRef) : base(reasonRef switch {
				Reason.NoHandle => expr,
				Reason.HasHandle => expr,
				Reason.WasFreed => expr,
				_ => throw new ArgumentOutOfRangeException(nameof(reasonRef), reasonRef, null),
		}) => ReasonRef = reasonRef;

		public enum Reason : byte {
			NoHandle = 0,
			HasHandle,
			WasFreed,
		}
	}
}