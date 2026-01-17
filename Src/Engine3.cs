using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine3.Exceptions;
using Engine3.Utils;

namespace Engine3 {
	public static partial class Engine3 {
		public const string Name = nameof(Engine3);
		public const bool Debug =
#if DEBUG
				true;
#else
				false;
#endif

		public static Version4 Version { get; } = new(0, 0, 0);
		public static Assembly Assembly => typeof(Engine3).Assembly;

		[field: MaybeNull]
		public static GameClient GameInstance { get => field ?? throw new Engine3Exception($"Attempted to get {nameof(GameInstance)} too early. Must call {nameof(GameClient)}#{nameof(GameClient.Start)} first"); internal set; }
	}
}