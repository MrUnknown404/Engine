using JetBrains.Annotations;
using USharpLibs.Common.IO;

namespace USharpLibs.Engine2.Client.GL.Models {
	[PublicAPI]
	public sealed class ModelAccess {
		internal Model? Model { get; set; }

		internal ModelAccess() { }

		public void Build() {
			if (Model == null) { throw new ModelAccessException(ModelAccessException.Reason.NothingBound); }
			if (Model.WasFreed) { throw new ModelAccessException(ModelAccessException.Reason.WasFreed); }
			if (Model.VAO == 0) { throw new ModelAccessException(ModelAccessException.Reason.NoVAO); }

			if (Model.IsBuildMeshEmpty) {
				switch (Model.IfBuildEmpty) {
					case Model.OnEmpty.SilentlyFail: return;
					case Model.OnEmpty.Scream:
						Logger.Warn("Attempted to build empty model.");
						return;
					case Model.OnEmpty.Throw: throw new ModelAccessException(ModelAccessException.Reason.NothingToBuild);
					default: throw new ArgumentOutOfRangeException();
				}
			}

			if (Model.CanBuild()) { Model.Build(); }
		}

		public void Draw() {
			if (Model == null) { throw new ModelAccessException(ModelAccessException.Reason.NothingBound); }
			if (Model.WasFreed) { throw new ModelAccessException(ModelAccessException.Reason.WasFreed); }
			if (Model.VAO == 0) { throw new ModelAccessException(ModelAccessException.Reason.NoVAO); }

			if (Model.IsDrawDataEmpty) {
				switch (Model.IfDrawEmpty) {
					case Model.OnEmpty.SilentlyFail: return;
					case Model.OnEmpty.Scream:
						Logger.Warn("Attempted to draw empty model.");
						return;
					case Model.OnEmpty.Throw: throw new ModelAccessException(ModelAccessException.Reason.NothingToDraw);
					default: throw new ArgumentOutOfRangeException();
				}
			}

			Model.Draw();
		}
	}
}