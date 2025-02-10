using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using USharpLibs.Common.IO;

namespace USharpLibs.Engine2.Debug {
	public class ErrorHandler<TSelf, TException> where TSelf : ErrorHandler<TSelf, TException>, IErrorHandler<TSelf, TException> where TException : Exception {
		[MustUseReturnValue] public static bool Assert([DoesNotReturnIf(true)] bool condition, Func<TSelf> errorInfo) => condition && Handle(errorInfo);

		[MustUseReturnValue]
		public static bool Handle(Func<TSelf> error) {
			switch (TSelf.ErrorMode) {
				case ErrorHandleMode.Ignore: return false;
				case ErrorHandleMode.Scream:
					Logger.Warn(TSelf.CreateMessage(error()));
					return true;
				case ErrorHandleMode.Throw: throw TSelf.CreateException(error());
				default: throw new ArgumentOutOfRangeException();
			}
		}
	}
}