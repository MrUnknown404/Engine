namespace USharpLibs.Engine2.Events {
	public class CancelableEventResult : IEventResult {
		public static IEventResult Empty { get; } = new CancelableEventResult();
		public bool ShouldCancel { get; set; }
	}
}