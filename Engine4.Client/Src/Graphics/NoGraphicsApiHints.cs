using OpenTK.Platform;

namespace Engine4.Client.Graphics;

public class NoGraphicsApiHints : GraphicsApiHints {
	public override OpenTK.Platform.GraphicsApi Api => OpenTK.Platform.GraphicsApi.None;
}