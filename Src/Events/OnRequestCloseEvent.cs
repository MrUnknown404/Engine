namespace USharpLibs.Engine2.Events {
	public class OnRequestCloseEvent : IEventResult {
		public static IEventResult Empty { get; } = new OnRequestCloseEvent();
		public bool ShouldClose { get; set; } = true;
	}
}