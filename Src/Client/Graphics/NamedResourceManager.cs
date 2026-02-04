namespace Engine3.Client.Graphics {
	public class NamedResourceManager<T> : ResourceManager<T> where T : INamedGraphicsResource, IEquatable<T> {
		protected override string GetDestroyMessage(T obj) => $"Destroying: {obj.DebugName}";
	}
}