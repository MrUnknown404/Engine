using JetBrains.Annotations;

namespace Engine4.Utility.Extensions;

public static class MulticastDelegateExtensions {
	extension(MulticastDelegate? self) {
		[MustUseReturnValue]
		public int GetInvocationListCount() => self?.GetInvocationList().Length ?? 0;
	}
}