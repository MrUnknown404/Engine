using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.Utils;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL.Models {
	[PublicAPI]
	public class KeyedModel<K> : Model where K : notnull {
		protected bool IsDirty;

		protected Dictionary<K, Mesh> Meshes { get; } = new();
		protected int IndicesLength;

		public KeyedModel(BufferUsageHint bufferHint) : base(bufferHint) { }

		public KeyedModel<K> AddMesh(in K key, Mesh mesh) {
			Meshes[key] = mesh;
			IsDirty = true;
			return this;
		}

		public KeyedModel<K> RemoveMesh(in K key) {
			Meshes.Remove(key);
			IsDirty = true;
			return this;
		}

		protected override void ISetupGL() {
			if (WasSetup) {
				Logger.Warn("This model was already setup!");
				return;
			}

			WasSetup = true;

			VAO = OpenGL4.GenVertexArray();
			VBO = OpenGL4.GenBuffer();
			EBO = OpenGL4.GenBuffer();

			if (Meshes.Count != 0) { RefreshModelData(); }
		}

		public void ClearModelData() {
			Meshes.Clear();
			IsDirty = true;
		}

		public void RefreshModelData() {
			if (IsDirty) {
				List<float> vertices = new();
				List<uint> indices = new();
				uint indexOffset = 0;

				foreach (Mesh part in Meshes.Values) {
					vertices.AddRange(part.Vertices);

					uint highestIndex = 0;
					foreach (uint i in part.Indices) {
						if (i > highestIndex) { highestIndex = i; }
						indices.Add(i + indexOffset);
					}

					indexOffset += highestIndex + 1;
				}

				IndicesLength = indices.Count;

				OpenGL4.BindVertexArray(VAO);

				OpenGL4.BindBuffer(BufferTarget.ArrayBuffer, VBO);
				OpenGL4.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferHint);

				OpenGL4.EnableVertexAttribArray(Shader.PositionLocation);
				OpenGL4.EnableVertexAttribArray(Shader.TextureLocation);
				OpenGL4.VertexAttribPointer(Shader.PositionLocation, 3, VertexAttribPointerType.Float, false, sizeof(float) * 5, 0);
				OpenGL4.VertexAttribPointer(Shader.TextureLocation, 2, VertexAttribPointerType.Float, false, sizeof(float) * 5, sizeof(float) * 3);

				OpenGL4.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
				OpenGL4.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferHint);

				OpenGL4.BindVertexArray(0);
				GLH.UnbindVAO();

				IsDirty = false;
			}
		}

		protected override void IDraw() => OpenGL4.DrawElements(PrimitiveType.Triangles, IndicesLength, DrawElementsType.UnsignedInt, 0);
	}
}