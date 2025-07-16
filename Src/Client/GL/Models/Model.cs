using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.IO;
using USharpLibs.Engine.Client.GL.Models.Vertex;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL.Models {
	[PublicAPI]
	public abstract class Model {
		protected BufferUsageHint BufferHint { get; }

		protected internal int VAO { get; set; } = -1;
		protected int VBO { get; set; }
		protected int EBO { get; set; }
		protected int IndicesLength { get; set; }
		public bool WasSetup { get; protected set; }

		protected abstract byte[] VertexArrangement { get; }
		protected abstract byte VertexTotalSize { get; }

		public abstract bool IsMeshEmpty { get; }
		protected bool IsIndicesEmpty => IndicesLength == 0;

		internal Model(BufferUsageHint bufferHint) => BufferHint = bufferHint;

		[MustUseReturnValue] protected abstract float[] ProcessMeshesIntoVertexArray();
		[MustUseReturnValue] protected abstract uint[] ProcessMeshesIntoIndexArray();

		public void SetupGL() {
			if (WasSetup) {
				Logger.Warn("This model was already setup!");
				return;
			}

			WasSetup = true;

			VAO = OpenGL4.GenVertexArray();
			VBO = OpenGL4.GenBuffer();
			EBO = OpenGL4.GenBuffer();

			OpenGL4.BindVertexArray(VAO);
			OpenGL4.BindBuffer(BufferTarget.ArrayBuffer, VBO);

			byte[] sizes = VertexArrangement;
			byte offset = 0;

			for (uint attribIndex = 0; attribIndex < sizes.Length; attribIndex++) {
				byte vertSize = sizes[attribIndex];

				OpenGL4.EnableVertexAttribArray(attribIndex);
				OpenGL4.VertexAttribPointer(attribIndex, vertSize, VertexAttribPointerType.Float, false, sizeof(float) * VertexTotalSize, sizeof(float) * offset);

				offset += vertSize;
			}

			if (!IsMeshEmpty) {
				GLH.Bind(this);
				BindModelData();
			}
		}

		// This assumes checks have already been checked
		internal void Draw() {
			if (!WasSetup) {
				Logger.Warn("Model was not setup! how? this was technically checked?");
				return;
			}

			IDraw();
		}

		protected virtual bool BindModelData() {
			if (GLH.CurrentModelVAO != VAO) {
				Logger.Warn("Attempted to modify VAO data with the wrong bound VAO.");
				return false;
			}

			float[] vertices = ProcessMeshesIntoVertexArray();
			uint[] indices = ProcessMeshesIntoIndexArray();

			IndicesLength = indices.Length;

			OpenGL4.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			OpenGL4.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferHint);

			OpenGL4.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
			OpenGL4.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferHint);

			return true;
		}

		/// <summary> Assumes the current VAO is us. </summary>
		protected virtual void IDraw() {
			if (IsIndicesEmpty || IsMeshEmpty) { return; }
			OpenGL4.DrawElements(PrimitiveType.Triangles, IndicesLength, DrawElementsType.UnsignedInt, 0);
		}
	}

	[PublicAPI]
	public abstract class Model<TVertex, TCollection> : Model where TVertex : IVertex where TCollection : ICollection<Mesh<TVertex>> {
		protected abstract TCollection Meshes { get; }

		protected override byte[] VertexArrangement => TVertex.Arrangement;
		protected override byte VertexTotalSize => TVertex.TotalSize;

		public override bool IsMeshEmpty => Meshes.Count == 0;

		// Is it worth caching raw vertex/index data? from ProcessMeshesIntoVertexArray/ProcessMeshesIntoIndexArray
		protected Model(BufferUsageHint bufferHint) : base(bufferHint) { }

		protected override float[] ProcessMeshesIntoVertexArray() {
			if (Meshes.Count == 0) {
				Logger.Warn("Attempted to process an empty model into raw data.");
				return Array.Empty<float>();
			}

			List<float> vertices = new();

			// I care about speed here. Hence no Linq
			foreach (Mesh<TVertex> mesh in Meshes) {
				foreach (TVertex vertex in mesh.Vertices) {
#pragma warning disable CS0618 // Type or member is obsolete
					vertex.Collect(vertices);
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}

			return vertices.ToArray();
		}

		[SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
		protected override uint[] ProcessMeshesIntoIndexArray() {
			if (Meshes.Count == 0) {
				Logger.Warn("Attempted to process an empty model into raw data.");
				return Array.Empty<uint>();
			}

			List<uint> indices = new();
			uint offset = 0;

			// I care about speed here. Hence no Linq
			foreach (Mesh<TVertex> mesh in Meshes) {
				if (mesh.AreIndicesGlobal) {
					foreach (uint index in mesh.Indices) { indices.Add(index); }
				} else {
					uint tempOffset = 0;

					foreach (uint index in mesh.Indices) {
						if (index > tempOffset) { tempOffset = index; }
						indices.Add(index + offset);
					}

					offset += tempOffset + 1;
				}
			}

			return indices.ToArray();
		}
	}
}