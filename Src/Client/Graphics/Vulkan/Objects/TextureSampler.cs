using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public sealed unsafe class TextureSampler : GraphicsResource<TextureSampler, ulong> {
		public VkSampler Sampler { get; }

		protected override ulong Handle => Sampler.Handle;

		private readonly VkDevice logicalDevice;

		internal TextureSampler(VkDevice logicalDevice, Settings settings) {
			this.logicalDevice = logicalDevice;

			VkSamplerCreateInfo samplerCreateInfo = new() {
					minFilter = settings.MinFilter,
					magFilter = settings.MagFilter,
					addressModeU = settings.AddressMode.U,
					addressModeV = settings.AddressMode.V,
					addressModeW = settings.AddressMode.W,
					anisotropyEnable =
							(int)(settings.AnisotropyEnable && (Engine3.GameInstance.GraphicsBackend as VulkanGraphicsBackend ?? throw new Engine3Exception("Wrong graphics api is in use")).AllowEnableAnisotropy ?
									Vk.True :
									Vk.False),
					maxAnisotropy = settings.MaxAnisotropy,
					borderColor = settings.BorderColor,
					unnormalizedCoordinates = (int)(settings.NormalizedCoordinates ? Vk.False : Vk.True),
					compareEnable = (int)Vk.False,
					compareOp = VkCompareOp.CompareOpAlways,
					mipmapMode = settings.MipmapMode,
					mipLodBias = settings.MipLodBias,
					minLod = settings.MinLod,
					maxLod = settings.MaxLod,
			};

			VkSampler textureSampler;
			VkH.CheckIfSuccess(Vk.CreateSampler(logicalDevice, &samplerCreateInfo, null, &textureSampler), VulkanException.Reason.CreateTextureSampler);
			Sampler = textureSampler;

			PrintCreate();
		}

		protected override void Cleanup() => Vk.DestroySampler(logicalDevice, Sampler, null);

		[PublicAPI]
		public class Settings {
			public VkFilter MinFilter { get; }
			public VkFilter MagFilter { get; }
			public float MaxAnisotropy { get; }

			public AddressMode AddressMode { get; init; }
			public VkBorderColor BorderColor { get; init; } = VkBorderColor.BorderColorIntOpaqueBlack;
			public bool NormalizedCoordinates { get; init; } = true;
			public bool AnisotropyEnable { get; init; } = true;

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
}