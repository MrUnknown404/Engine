using Engine3.Utility;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class TextureSampler : IDestroyable {
		public VkSampler Sampler { get; }

		public bool WasDestroyed { get; private set; }

		private readonly VkDevice logicalDevice;

		internal TextureSampler(VkDevice logicalDevice, VkSampler sampler) {
			this.logicalDevice = logicalDevice;
			Sampler = sampler;
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			Vk.DestroySampler(logicalDevice, Sampler, null);

			WasDestroyed = true;
		}

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