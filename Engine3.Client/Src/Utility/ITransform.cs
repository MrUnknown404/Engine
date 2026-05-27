using System.Numerics;

namespace Engine3.Client.Utility;

public interface ITransform<out T> where T : ITransform<T>, IEquatable<T> { // TODO automate storing the previous state at the end of an update?
	public static abstract T Zero { get; }

	public Matrix4x4 CreateMatrix();
}