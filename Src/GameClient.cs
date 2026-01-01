using System.Diagnostics;
using System.Reflection;
using Engine3.Exceptions;
using Engine3.Utils;
using JetBrains.Annotations;
using NLog;
using OpenTK.Platform;
using GraphicsApi = Engine3.Graphics.GraphicsApi;

namespace Engine3 {
	public abstract class GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public Assembly Assembly { get; internal init; } = null!; // Set in Engine3#Start

		public abstract Version4 Version { get; }

		protected internal abstract void Setup();

		protected internal abstract void Update();
		protected internal abstract void Render(float delta);

		[MustUseReturnValue]
		protected static EngineWindow CreateWindow(string title, int w, int h) {
			if (Engine3.GraphicsApi == GraphicsApi.Console) { throw new IllegalStateException("Cannot create window with no graphics api"); }

			WindowHandle windowHandle = Toolkit.Window.Create(Engine3.GraphicsApiHints ?? throw new UnreachableException());
			Logger.Debug("Created new window");

			Toolkit.Window.SetTitle(windowHandle, title);
			Toolkit.Window.SetSize(windowHandle, new(w, h));

			EngineWindow window = new(windowHandle);
			Engine3.Windows.Add(window);
			return window;
		}
	}
}