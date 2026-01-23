using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using Engine3.Utils;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using Silk.NET.Shaderc;

namespace Engine3.Graphics.Vulkan.Objects {
	public unsafe class VkShaderObject : IGraphicsResource {
		public VkShaderModule ShaderModule { get; }
		public ShaderType ShaderType { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly VkDevice logicalDevice;

		public VkShaderObject(string debugName, VkDevice logicalDevice, string fileLocation, ShaderLanguage shaderLang, ShaderType shaderType, Assembly assembly) {
			DebugName = debugName;
			this.logicalDevice = logicalDevice;
			ShaderModule = CreateShaderModule(logicalDevice, fileLocation, shaderLang, shaderType, assembly);
			ShaderType = shaderType;
		}

		public void Destroy() {
			if (IGraphicsResource.CheckIfDestroyed(this)) { return; }

			Vk.DestroyShaderModule(logicalDevice, ShaderModule, null);

			WasDestroyed = true;
		}

		[MustUseReturnValue]
		private static VkShaderModule CreateShaderModule(VkDevice logicalDevice, string fileLocation, ShaderLanguage shaderLang, ShaderType shaderType, Assembly assembly) {
			string fullFileName = $"{Engine3.GameInstance.GraphicsApi}.{fileLocation}.{shaderType.FileExtension}.{shaderLang.FileExtension}";

			using Stream? shaderStream = AssetH.GetAssetStream($"Shaders.{fullFileName}", assembly);
			if (shaderStream == null) { throw new Engine3Exception($"Failed to create asset stream at Shaders.{fullFileName}"); }

			switch (shaderLang) {
				case ShaderLanguage.Glsl or ShaderLanguage.Hlsl: {
					Shaderc shaderc = Engine3.GameInstance.Shaderc;

					Compiler* compiler = shaderc.CompilerInitialize();
					CompileOptions* options = shaderc.CompileOptionsInitialize();

					shaderc.CompileOptionsSetSourceLanguage(options, shaderLang switch {
							ShaderLanguage.Glsl => SourceLanguage.Glsl,
							ShaderLanguage.Hlsl => SourceLanguage.Hlsl,
							ShaderLanguage.SpirV => throw new UnreachableException(),
							_ => throw new NotImplementedException(),
					});

					using StreamReader streamReader = new(shaderStream);
					string source = streamReader.ReadToEnd();
					byte* sourcePtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(source)));
					byte* shaderNamePtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(fullFileName)));
					ShaderKind shaderKind = shaderType switch {
							ShaderType.Fragment => ShaderKind.FragmentShader,
							ShaderType.Vertex => ShaderKind.VertexShader,
							ShaderType.Geometry => ShaderKind.GeometryShader,
							ShaderType.TessEvaluation => ShaderKind.TessEvaluationShader,
							ShaderType.TessControl => ShaderKind.TessControlShader,
							ShaderType.Compute => ShaderKind.ComputeShader,
							_ => throw new ArgumentOutOfRangeException(nameof(shaderType), shaderType, null),
					};

					CompilationResult* compilationResult = shaderc.CompileIntoSpv(compiler, sourcePtr, (nuint)source.Length, shaderKind, shaderNamePtr, "main", options);
					shaderc.CompileOptionsRelease(options);

					CompilationStatus status = shaderc.ResultGetCompilationStatus(compilationResult);
					shaderc.CompilerRelease(compiler);

					if (status != CompilationStatus.Success) {
						shaderc.ResultRelease(compilationResult);
						throw new Engine3Exception($"Failed to compile {shaderType} shader: {fileLocation}. {shaderc.ResultGetErrorMessageS(compilationResult)}");
					}

					VkShaderModuleCreateInfo shaderModuleCreateInfo = new() { codeSize = shaderc.ResultGetLength(compilationResult), pCode = (uint*)shaderc.ResultGetBytes(compilationResult), };
					VkShaderModule shaderModule;
					VkResult result = Vk.CreateShaderModule(logicalDevice, &shaderModuleCreateInfo, null, &shaderModule);

					shaderc.ResultRelease(compilationResult);

					return result != VkResult.Success ? throw new VulkanException($"Failed to create shader module. {result}") : shaderModule;
				}
				case ShaderLanguage.SpirV: {
					using BinaryReader reader = new(shaderStream);
					byte[] data = reader.ReadBytes((int)shaderStream.Length);

					fixed (byte* shaderCodePtr = data) {
						VkShaderModuleCreateInfo shaderModuleCreateInfo = new() { codeSize = (UIntPtr)data.Length, pCode = (uint*)shaderCodePtr, };
						VkShaderModule shaderModule;
						VkResult result = Vk.CreateShaderModule(logicalDevice, &shaderModuleCreateInfo, null, &shaderModule);
						return result != VkResult.Success ? throw new VulkanException($"Failed to create shader module. {result}") : shaderModule;
					}
				}
				default: throw new ArgumentOutOfRangeException(nameof(shaderLang), shaderLang, null);
			}
		}
	}
}