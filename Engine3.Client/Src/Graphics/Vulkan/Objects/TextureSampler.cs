using Engine3.Client.Utility.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects;

public sealed unsafe class TextureSampler : GraphicsResource<TextureSampler, ulong> {
	public VkSampler Sampler { get; }

	protected override ulong Handle => Sampler.Handle;

	private readonly LogicalGpu logicalGpu;

	internal TextureSampler(LogicalGpu logicalGpu, Settings settings) {
		this.logicalGpu = logicalGpu;
		Sampler = CreateSampler(logicalGpu, settings);

		PrintCreate();
	}

	protected override void Cleanup() => Vk.DestroySampler(logicalGpu.LogicalDevice, Sampler, null);

	[MustUseReturnValue]
	private static VkSampler CreateSampler(LogicalGpu logicalGpu, Settings settings) {
		VkSamplerCreateInfo samplerCreateInfo = new() {
				minFilter = settings.MinFilter,
				magFilter = settings.MagFilter,
				addressModeU = settings.AddressMode.U,
				addressModeV = settings.AddressMode.V,
				addressModeW = settings.AddressMode.W,
				anisotropyEnable = settings.EnableAnisotropy && ((VulkanBackend?)((Engine3Client)Core.Engine3.Engine).GraphicsBackend ?? throw new NullReferenceException()).Settings.AllowEnableAnisotropy ? VkH.True : VkH.False,
				maxAnisotropy = settings.MaxAnisotropy,
				borderColor = settings.BorderColor,
				unnormalizedCoordinates = settings.NormalizedCoordinates ? VkH.False : VkH.True,
				compareEnable = VkH.False,
				compareOp = VkCompareOp.CompareOpAlways,
				mipmapMode = settings.MipmapMode,
				mipLodBias = settings.MipLodBias,
				minLod = settings.MinLod,
				maxLod = settings.MaxLod,
		};

		VkSampler textureSampler;
		VkH.CheckIfSuccess(Vk.CreateSampler(logicalGpu.LogicalDevice, &samplerCreateInfo, null, &textureSampler), VulkanException.Reason.CreateTextureSampler);
		return textureSampler;
	}

	[PublicAPI]
	public class Settings {
		public VkFilter MinFilter { get; }
		public VkFilter MagFilter { get; }
		public float MaxAnisotropy { get; }

		public AddressMode AddressMode { get; init; }
		public VkBorderColor BorderColor { get; init; } = VkBorderColor.BorderColorIntOpaqueBlack;
		public bool NormalizedCoordinates { get; init; } = true;
		public bool EnableAnisotropy { get; init; } = true;

		public VkSamplerMipmapMode MipmapMode { get; private set; } = VkSamplerMipmapMode.SamplerMipmapModeLinear;
		public float MipLodBias { get; private set; }
		public float MinLod { get; private set; }
		public float MaxLod { get; private set; }

		public Settings(VkFilter minFilter, VkFilter magFilter, float maxAnisotropy) {
			MinFilter = minFilter;
			MagFilter = magFilter;
			MaxAnisotropy = maxAnisotropy;
		}

		public Settings(VkFilter minFilter, VkFilter magFilter, VkPhysicalDeviceLimits physicalDeviceLimits) : this(minFilter, magFilter, physicalDeviceLimits.maxSamplerAnisotropy) { }

		public Settings SetMipmapMode(VkSamplerMipmapMode mipmapMode, float mipLodBias, float minLod, float maxLod) {
			MipmapMode = mipmapMode;
			MipLodBias = mipLodBias;
			MinLod = minLod;
			MaxLod = maxLod;
			return this;
		}
	}

	public readonly record struct AddressMode {
		public VkSamplerAddressMode U { get; init; } = VkSamplerAddressMode.SamplerAddressModeRepeat;
		public VkSamplerAddressMode V { get; init; } = VkSamplerAddressMode.SamplerAddressModeRepeat;
		public VkSamplerAddressMode W { get; init; } = VkSamplerAddressMode.SamplerAddressModeRepeat;

		public AddressMode(VkSamplerAddressMode u, VkSamplerAddressMode v, VkSamplerAddressMode w) {
			U = u;
			V = v;
			W = w;
		}
	}
}