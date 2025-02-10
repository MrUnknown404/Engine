using JetBrains.Annotations;
using USharpLibs.Common.IO;
using USharpLibs.Engine2.Debug;
using USharpLibs.Engine2.Exceptions;

namespace USharpLibs.Engine2.Client.Models {
	[PublicAPI]
	public sealed class ModelAccess {
		internal Model? Model { get; set; }

		internal ModelAccess() { }

		public void Build() {
			if (ModelAccessErrorHandler.Assert(Model == null, static () => new(ModelAccessErrorHandler.Reason.NothingBound))) { return; }
			if (ModelErrorHandler.Assert(Model.WasFreed, static () => new(ModelErrorHandler.Reason.WasFreed))) { return; }

			if (Model.IsBuildMeshEmpty) {
				switch (Model.IfBuildEmpty) {
					case Model.OnEmpty.SilentlyFail: return;
					case Model.OnEmpty.Scream:
						Logger.Warn("Attempted to build empty model.");
						return;
					case Model.OnEmpty.Throw: throw new ModelAccessException(ModelAccessErrorHandler.CreateMessage(new(ModelAccessErrorHandler.Reason.NothingToBuild)));
					default: throw new ArgumentOutOfRangeException();
				}
			}

			if (Model.CanBuild()) { Model.Build(); }
		}

		public void Draw() {
			if (ModelAccessErrorHandler.Assert(Model == null, static () => new(ModelAccessErrorHandler.Reason.NothingBound))) { return; }
			if (ModelErrorHandler.Assert(Model.WasFreed, static () => new(ModelErrorHandler.Reason.WasFreed))) { return; }

			if (Model.IsDrawDataEmpty) {
				switch (Model.IfDrawEmpty) {
					case Model.OnEmpty.SilentlyFail: return;
					case Model.OnEmpty.Scream:
						Logger.Warn("Attempted to draw empty model.");
						return;
					case Model.OnEmpty.Throw: throw new ModelAccessException(ModelAccessErrorHandler.CreateMessage(new(ModelAccessErrorHandler.Reason.NothingToDraw)));
					default: throw new ArgumentOutOfRangeException();
				}
			}

			Model.Draw();
		}
	}
}