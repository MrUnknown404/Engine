using Engine3.Client.Model;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;

namespace Engine3.Client {
	public static class GLH {
		public static bool IsWireframe { get; private set; }
		public static bool IsDepthTesting { get; private set; }
		public static bool IsCulling { get; private set; }
		public static bool IsBlend { get; private set; }
		public static BlendingFactor SFactor { get; private set; } = BlendingFactor.One;
		public static BlendingFactor DFactor { get; private set; } = BlendingFactor.Zero;

		public static ClearBufferMask ClearBufferMask { get; set; } = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit;

		private static uint shader;
		private static uint vao;
		private static int vaoDrawSize;

		[MustUseReturnValue]
		public static T Bind<T>(Shader<T> shader) where T : ShaderContext, new() {
			if (shader.Handle == 0) { throw new Exception(); } // TODO exception

			if (GLH.shader != shader.Handle) {
				GLH.shader = shader.Handle;
				GL.UseProgram(GLH.shader);
			}

			return shader.Context;
		}

		public static void Bind(VertexArrayObject vao) {
			if (!vao.WereBuffersCreated) { throw new Exception(); } // TODO exception
			if (vao.WasFreed) { throw new Exception(); } // TODO exception

			if (vao.Vao != GLH.vao) {
				GLH.vao = vao.Vao;
				vaoDrawSize = vao.IndexCount;
				GL.BindVertexArray(vao.Vao);
			}
		}

		public static void UnbindShader() {
			if (shader == 0) { return; }
			shader = 0;
			GL.UseProgram(0);
		}

		public static void UnbindVao() {
			if (vao == 0) { return; }
			vao = 0;
			GL.BindVertexArray(0);
		}

		public static void Draw() {
			if (shader == 0) { throw new Exception(); } // TODO exception
			if (vao == 0) { throw new Exception(); } // TODO exception

			GL.DrawElements(PrimitiveType.Triangles, vaoDrawSize, DrawElementsType.UnsignedInt, 0);
		}

		/// <summary> Enables Wireframe mode. </summary>
		public static void EnableWireframe() {
			if (!IsWireframe) {
				GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
				IsWireframe = true;
			}
		}

		/// <summary> Disables Wireframe mode. </summary>
		public static void DisableWireframe() {
			if (IsWireframe) {
				GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
				IsWireframe = false;
			}
		}

		/// <summary> Enables Depth Testing. </summary>
		public static void EnableDepthTest() {
			if (!IsDepthTesting) {
				GL.Enable(EnableCap.DepthTest);
				IsDepthTesting = true;
			}
		}

		/// <summary> Disables Depth Testing. </summary>
		public static void DisableDepthTest() {
			if (IsDepthTesting) {
				GL.Disable(EnableCap.DepthTest);
				IsDepthTesting = false;
			}
		}

		/// <summary> Enables Culling. </summary>
		public static void EnableCulling() {
			if (!IsCulling) {
				GL.Enable(EnableCap.CullFace);
				IsCulling = true;
			}
		}

		/// <summary> Disables Culling. </summary>
		public static void DisableCulling() {
			if (IsCulling) {
				GL.Disable(EnableCap.CullFace);
				IsCulling = false;
			}
		}

		public static void EnableBlend() {
			if (!IsBlend) {
				GL.Enable(EnableCap.Blend);
				IsBlend = true;
			}
		}

		public static void EnableBlend(BlendingFactor sFactor, BlendingFactor dFactor) {
			if (!IsBlend) {
				GL.Enable(EnableCap.Blend);
				IsBlend = true;
				SetBlendFunc(sFactor, dFactor);
			}
		}

		public static void DisableBlend() {
			if (IsBlend) {
				GL.Disable(EnableCap.Blend);
				IsBlend = false;
			}
		}

		public static void SetBlendFunc(BlendingFactor sFactor, BlendingFactor dFactor) {
			if (sFactor != SFactor || dFactor != DFactor) {
				GL.BlendFunc(sFactor, dFactor);
				SFactor = sFactor;
				DFactor = dFactor;
			}
		}
	}
}