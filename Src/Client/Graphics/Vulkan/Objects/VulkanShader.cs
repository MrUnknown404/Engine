using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Utility;
using Engine3.Utility.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using Silk.NET.Shaderc;

namespace Engine3.Client.Graphics.Vulkan.Objects;

[PublicAPI]
public sealed unsafe class VulkanShader : NamedGraphicsResource<VulkanShader, ulong> {
	public VkShaderModule ShaderModule { get; }
	public ShaderType ShaderType { get; }
	public VkSpecializationInfo? SpecializationInfo { get; }

	protected override ulong Handle => ShaderModule.Handle;

	private readonly LogicalGpu logicalGpu;

	internal VulkanShader(string debugName, LogicalGpu logicalGpu, string fileLocation, ShaderLanguage shaderLang, ShaderType shaderType, VkSpecializationInfo? specializationInfo, Assembly assembly) : base(debugName) {
		ShaderModule = CreateShaderModule(logicalGpu, fileLocation, shaderLang, shaderType, assembly);
		ShaderType = shaderType;
		SpecializationInfo = specializationInfo;
		this.logicalGpu = logicalGpu;

		PrintCreate();
	}

	protected override void Cleanup() => Vk.DestroyShaderModule(logicalGpu.LogicalDevice, ShaderModule, null);

	[MustUseReturnValue]
	private static VkShaderModule CreateShaderModule(LogicalGpu logicalGpu, string fileLocation, ShaderLanguage shaderLang, ShaderType shaderType, Assembly assembly) {
		string fullFileName = $"{GraphicsBackend.Vulkan}.{fileLocation}.{shaderType.FileExtension}.{shaderLang.FileExtension}";

		using Stream? shaderStream = AssetH.GetAssetStream($"Shaders.{fullFileName}", assembly);
		if (shaderStream == null) { throw new Engine3Exception($"Failed to create asset stream at Shaders.{fullFileName}"); }

		return shaderLang switch {
				ShaderLanguage.Glsl or ShaderLanguage.Hlsl => CompileShaderModule(),
				ShaderLanguage.SpirV => LoadShaderModule(),
				_ => throw new ArgumentOutOfRangeException(nameof(shaderLang), shaderLang, null),
		};

		VkShaderModule CompileShaderModule() {
			Shaderc shaderc = Engine3.Engine.Shaderc;

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
				string errorMessage = shaderc.ResultGetErrorMessageS(compilationResult);
				shaderc.ResultRelease(compilationResult);
				throw new Engine3Exception($"Failed to compile {shaderType} shader: {fileLocation}. {errorMessage}");
			}

			VkShaderModuleCreateInfo shaderModuleCreateInfo = new() { codeSize = shaderc.ResultGetLength(compilationResult), pCode = (uint*)shaderc.ResultGetBytes(compilationResult), };
			VkShaderModule shaderModule;
			VkResult result = Vk.CreateShaderModule(logicalGpu.LogicalDevice, &shaderModuleCreateInfo, null, &shaderModule);

			shaderc.ResultRelease(compilationResult);

			VkH.CheckIfSuccess(result, VulkanException.Reason.CreateShaderModule);

			return shaderModule;
		}

		VkShaderModule LoadShaderModule() {
			using BinaryReader reader = new(shaderStream);
			byte[] data = reader.ReadBytes((int)shaderStream.Length);

			fixed (byte* shaderCodePtr = data) {
				VkShaderModuleCreateInfo shaderModuleCreateInfo = new() { codeSize = (UIntPtr)data.Length, pCode = (uint*)shaderCodePtr, };
				VkShaderModule shaderModule;
				VkH.CheckIfSuccess(Vk.CreateShaderModule(logicalGpu.LogicalDevice, &shaderModuleCreateInfo, null, &shaderModule), VulkanException.Reason.CreateShaderModule);
				return shaderModule;
			}
		}
	}
}