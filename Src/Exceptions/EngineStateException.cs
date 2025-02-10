namespace USharpLibs.Engine2.Exceptions {
	public class EngineStateException : Exception {
		public EngineStateException(Reason reason) : base(reason switch {
				Reason.EngineStartNotCalled => "GameEngine#Start must be called before accessing this.",
				Reason.EngineStartAlreadyCalled => "GameEngine#Start cannot be called twice.",
				_ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
		}) { }

		public enum Reason : byte {
			EngineStartNotCalled = 0,
			EngineStartAlreadyCalled,
		}
	}
}