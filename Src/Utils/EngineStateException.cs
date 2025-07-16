namespace Engine3.Utils {
	public class EngineStateException : Exception, IReasonEnumException<EngineStateException.Reason> {
		public Reason ReasonRef { get; }

		public EngineStateException(Reason reasonRef) : base(reasonRef switch {
				Reason.AlreadySetValue => expr,
				Reason.StartNotCalled => expr,
				_ => throw new ArgumentOutOfRangeException(nameof(reasonRef), reasonRef, null),
		}) => ReasonRef = reasonRef;

		public enum Reason : byte {
			AlreadySetValue = 0,
			StartNotCalled,
		}
	}
}