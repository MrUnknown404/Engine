using System.Reflection;
using Engine3.Utils;

namespace Engine3 {
	public abstract partial class GameClient { // TODO split GL/VK?
		public abstract Version4 Version { get; }
		public Assembly Assembly { get; internal init; } = null!; // Set in Engine3#Start
		public string Name { get; internal init; } = null!; // Set in Engine3#Start

		protected internal abstract void Setup();
		protected internal abstract void Update();

		protected internal abstract void Cleanup();
	}
}