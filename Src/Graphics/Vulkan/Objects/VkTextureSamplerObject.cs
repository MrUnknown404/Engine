using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan.Objects {
	public unsafe class VkTextureSamplerObject : IGraphicsResource {
		public VkSampler TextureSampler { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly VkDevice logicalDevice;

		public VkTextureSamplerObject(VkDevice logicalDevice, Settings settings) {
			DebugName = settings.DebugName;
			TextureSampler = CreateTextureSampler(logicalDevice, settings);
			this.logicalDevice = logicalDevice;
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			Vk.DestroySampler(logicalDevice, TextureSampler, null);

			WasDestroyed = true;
		}

		[MustUseReturnValue]
		private static VkSampler CreateTextureSampler(VkDevice logicalDevice, Settings settings) {
			VkSamplerCreateInfo samplerCreateInfo = new() {
					minFilter = settings.MinFilter,
					magFilter = settings.MagFilter,
					addressModeU = settings.AddressMode.U,
					addressModeV = settings.AddressMode.V,
					addressModeW = settings.AddressMode.W,
					anisotropyEnable = (int)(settings.AnisotropyEnable && Engine3.GameInstance.AllowEnableAnisotropy ? Vk.True : Vk.False),
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
			return textureSampler;
		}

		[PublicAPI]
		public class Settings {
			public string DebugName { get; }
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

			public Settings(string debugName, VkFilter minFilter, VkFilter magFilter, float maxAnisotropy) {
				DebugName = debugName;
				MinFilter = minFilter;
				MagFilter = magFilter;
				MaxAnisotropy = maxAnisotropy;
			}

			public Settings(string debugName, VkFilter minFilter, VkFilter magFilter, VkPhysicalDeviceLimits physicalDeviceLimits) : this(debugName, minFilter, magFilter, physicalDeviceLimits.maxSamplerAnisotropy) { }

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