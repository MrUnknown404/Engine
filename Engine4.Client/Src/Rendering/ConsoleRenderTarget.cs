namespace Engine4.Client.Rendering;

public class ConsoleRenderTarget : RenderTarget {
	public bool UseVulkan { get; }

	public ConsoleRenderTarget(bool useVulkan) => UseVulkan = useVulkan;
}