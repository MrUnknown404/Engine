using OpenTK.Platform;

namespace Engine4.Client.Graphics.Software;

public class SoftwareGraphicsApiHints : GraphicsApiHints {
	public override OpenTK.Platform.GraphicsApi Api => OpenTK.Platform.GraphicsApi.None;
}