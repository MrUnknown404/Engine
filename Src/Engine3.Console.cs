using System.Diagnostics.CodeAnalysis;
using Engine3.Graphics;

namespace Engine3 {
	public static partial class Engine3 {
		[SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
		public class ConsoleSettings : StartupSettings {
			public override GraphicsApi GraphicsApi => GraphicsApi.Console;

			[SetsRequiredMembers] public ConsoleSettings(string gameName) : base(gameName) { }
		}
	}
}