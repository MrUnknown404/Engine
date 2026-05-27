using Engine3.Core.Utility.Exceptions;

namespace Engine3.Client.Utility.Exceptions;

public class OpenGLException : Exception, IEnumException<OpenGLException.Reason> {
	public Reason ReasonValue { get; }

	public OpenGLException(Reason reason) : base(ReasonToString(reason)) => ReasonValue = reason;
	public OpenGLException(Reason reason, params object?[] args) : base(string.Format(ReasonToString(reason), args)) => ReasonValue = reason;

	public static string ReasonToString(Reason reason) =>
			reason switch {
					Reason.Unknown => "Unknown error type",
					Reason.ShaderCompileFail => "Failed to compile shader: {0}. Reason: {1}",
					_ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
			};

	public enum Reason : uint {
		Unknown = 0,
		ShaderCompileFail,
	}
}