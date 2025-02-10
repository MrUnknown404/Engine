namespace USharpLibs.Engine2.Debug {
	public interface IErrorHandler<in TSelf, out TException> where TSelf : ErrorHandler<TSelf, TException>, IErrorHandler<TSelf, TException> where TException : Exception {
		public static abstract ErrorHandleMode ErrorMode { get; set; }

		public static abstract string CreateMessage(TSelf error);
		public static abstract TException CreateException(TSelf error);
	}
}