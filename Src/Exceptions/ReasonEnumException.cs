using JetBrains.Annotations;

namespace Engine3.Exceptions {
	public abstract class ReasonEnumException<T> : Exception where T : unmanaged, Enum {
		public T ReasonRef { get; }

		protected ReasonEnumException(T reasonRef, [RequireStaticDelegate] Func<T, string> reasonToString) : base(reasonToString(reasonRef)) => ReasonRef = reasonRef;
	}
}