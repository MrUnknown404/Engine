using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine2.Client.Models {
	public abstract class ModelBase<TBufferData, TMeshes, TMesh> : Model
			where TBufferData : IList<MeshBufferData>
			where TMeshes : IList<TMesh>
			where TMesh : IMesh {
		protected TBufferData MeshBufferData { get; }
		protected TMeshes Meshes { get; }

		protected ModelBase(TBufferData meshBufferData, TMeshes meshes) {
			MeshBufferData = meshBufferData;
			Meshes = meshes;
		}

		protected internal override void Draw() {
			foreach (MeshBufferData data in MeshBufferData) {
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, data.EBO);
				GL.DrawElements(PrimitiveType.Triangles, data.Count, DrawElementsType.UnsignedInt, 0);
			}
		}
	}
}