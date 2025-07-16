using Engine3.Client.Model;
using Engine3.Utils;

namespace Engine3.Client {
	public class GLBufferException : Exception, IReasonEnumException<GLBufferException.Reason> {
		public Reason ReasonRef { get; }

		public GLBufferException(Reason reasonRef) : base(reasonRef switch {
				Reason.WasFreed => $"{nameof(GLBuffer)} was freed so OpenGL methods will no longer work",
				Reason.HasHandle => $"{nameof(GLBuffer)} handle has already been set",
				Reason.BufferResizeError => $"Attempted to resize a {nameof(GLBuffer)} that does not support resizing",
				Reason.SwitchedStorageType => $"Attempted to use a method that uses a different {nameof(GLBuffer)} storage type then the one that is currently in use",
				_ => throw new ArgumentOutOfRangeException(nameof(reasonRef), reasonRef, null),
		}) => ReasonRef = reasonRef;

		public enum Reason : byte {
			WasFreed = 0,
			HasHandle,
			BufferResizeError,
			SwitchedStorageType,
		}
	}
}