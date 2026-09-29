using System.Reflection;
using Engine4.Client.Utility;
using Engine4.Utility;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using Silk.NET.Shaderc;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class VulkanShader : VulkanResource {
	public const string ShaderEntryPoint = "main";

	internal VkShaderModule ShaderModule { get; }
	internal ShaderType ShaderType { get; }
	internal VkSpecializationInfo? SpecializationInfo { get; }

	protected override ulong Handle => ShaderModule.Handle;

	private readonly LogicalGpu logicalGpu;

	internal VulkanShader(string debugName, Shaderc shaderc, LogicalGpu logicalGpu, Assembly assembly, string fileLocation, ShaderLanguage shaderLang, ShaderType shaderType,
		VkSpecializationInfo? specializationInfo) : base(debugName) {
		this.logicalGpu = logicalGpu;
		ShaderModule = CreateShaderModule(shaderc, logicalGpu, fileLocation, assembly, shaderLang, shaderType, debugName);
		ShaderType = shaderType;
		SpecializationInfo = specializationInfo;
	}

	protected internal override void Cleanup() => Vk.DestroyShaderModule(logicalGpu.VkLogicalDevice, ShaderModule, null);

	[MustUseReturnValue]
	private static VkShaderModule CreateShaderModule(Shaderc shaderc, LogicalGpu logicalGpu, string fileLocation, Assembly assembly, ShaderLanguage shaderLang, ShaderType shaderType, string debugName) {
		using Stream? shaderStream = AssetH.GetAssetStream($"{fileLocation}.{shaderType.FileExtension}.{shaderLang.FileExtension}", assembly);
		if (shaderStream == null) { throw new Exception(); } // TODO exception

		return shaderLang switch {
				ShaderLanguage.Glsl or ShaderLanguage.Hlsl => CompileShader(shaderc, logicalGpu, shaderStream, debugName, shaderLang, shaderType),
				ShaderLanguage.SpirV => LoadShader(logicalGpu, shaderStream),
				_ => throw new ArgumentOutOfRangeException(nameof(shaderLang), shaderLang, null),
		};

		[MustUseReturnValue]
		static VkShaderModule CompileShader(Shaderc shaderc, LogicalGpu logicalGpu, Stream shaderStream, string debugName, ShaderLanguage shaderLang, ShaderType shaderType) {
			using StreamReader streamReader = new(shaderStream);
			string source = streamReader.ReadToEnd();

			using StringUtf8Ptr pSource = new(source);
			using StringUtf8Ptr pShaderName = new(debugName);

			Compiler* compiler = shaderc.CompilerInitialize();
			CompileOptions* options = shaderc.CompileOptionsInitialize();
			shaderc.CompileOptionsSetSourceLanguage(options, shaderLang.Shaderc_SourceLanguage);

			CompilationResult* compilationResult = shaderc.CompileIntoSpv(compiler, pSource, (nuint)source.Length, shaderType.Shaderc_ShaderKind, pShaderName, ShaderEntryPoint, options);
			shaderc.CompileOptionsRelease(options);

			CompilationStatus compilationStatus = shaderc.ResultGetCompilationStatus(compilationResult);
			shaderc.CompilerRelease(compiler);

			if (compilationStatus != CompilationStatus.Success) {
				string errorMessage = shaderc.ResultGetErrorMessageS(compilationResult);
				shaderc.ResultRelease(compilationResult);
				throw new Exception($"Failed to compile {shaderType} shader: {debugName}. {errorMessage}"); // TODO exception
			}

			VkShaderModuleCreateInfo shaderModuleCreateInfo = new() { codeSize = shaderc.ResultGetLength(compilationResult), pCode = (uint*)shaderc.ResultGetBytes(compilationResult), };
			VkShaderModule shaderModule;
			VkResult result = Vk.CreateShaderModule(logicalGpu.VkLogicalDevice, &shaderModuleCreateInfo, null, &shaderModule);

			shaderc.ResultRelease(compilationResult);

			return VkH.CheckSuccess(result, "Failed to create shader module") ? shaderModule : throw new Exception(); // TODO exception
		}

		[MustUseReturnValue]
		static VkShaderModule LoadShader(LogicalGpu logicalGpu, Stream shaderStream) {
			using BinaryReader reader = new(shaderStream);
			byte[] shaderData = reader.ReadBytes((int)shaderStream.Length);

			fixed (byte* pShaderData = shaderData) {
				VkShaderModuleCreateInfo shaderModuleCreateInfo = new() { codeSize = (nuint)shaderData.Length, pCode = (uint*)pShaderData, };
				VkShaderModule shaderModule;
				return VkH.CheckSuccess(Vk.CreateShaderModule(logicalGpu.VkLogicalDevice, &shaderModuleCreateInfo, null, &shaderModule), "Failed to create shader module") ?
						shaderModule :
						throw new Exception(); // TODO exception
			}
		}
	}
}