using System.Diagnostics.CodeAnalysis;
using Engine3.Utils;
using OpenTK.Platform;
using GraphicsApi = Engine3.Graphics.GraphicsApi;

namespace Engine3 {
	public static partial class Engine3 {
		[SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
		public class OpenGLSettings : StartupSettings {
			public required ToolkitOptions ToolkitOptions { get; init; }
			public required OpenGLGraphicsApiHints GraphicsApiHints { get; init; }
			public override GraphicsApi GraphicsApi => GraphicsApi.OpenGL;

			[SetsRequiredMembers]
			public OpenGLSettings(string gameName, string mainThreadName, ToolkitOptions toolkitOptions, OpenGLGraphicsApiHints? graphicsApiHints = null) : base(gameName, mainThreadName) {
				ToolkitOptions = toolkitOptions;
				GraphicsApiHints = graphicsApiHints ?? new();

				ToolkitOptions.Logger = new TkLogger();
				ToolkitOptions.FeatureFlags = ToolkitFlags.EnableOpenGL;

				GraphicsApiHints.Version = new(4, 6);
				GraphicsApiHints.Profile = OpenGLProfile.Core;
#if DEBUG
				GraphicsApiHints.DebugFlag = true;
#endif
			}
		}
	}
}