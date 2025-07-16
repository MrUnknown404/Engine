namespace Engine3.Exceptions {
	public class EngineStateException : ReasonEnumException<EngineStateException.Reason> {
		public EngineStateException(Reason reasonRef) : base(reasonRef, static r => r switch {
				Reason.StartNotCalled => $"Value is null because {nameof(GameEngine)}#{nameof(GameEngine.Start)} was not called",
				Reason.NoGraphicsApi => "Attempted to use graphics with no graphics api selected",
				_ => throw new ArgumentOutOfRangeException(nameof(r), r, null),
		}) { }

		public enum Reason : byte {
			StartNotCalled = 0,
			NoGraphicsApi,
		}
	}
}