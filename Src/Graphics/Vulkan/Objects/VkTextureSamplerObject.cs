using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan.Objects {
	public unsafe class VkTextureSamplerObject : IGraphicsResource {
		public VkSampler TextureSampler { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly VkDevice logicalDevice;

		private VkTextureSamplerObject(string debugName, VkDevice logicalDevice, VkSampler textureSampler) {
			DebugName = debugName;
			this.logicalDevice = logicalDevice;
			TextureSampler = textureSampler;
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			Vk.DestroySampler(logicalDevice, TextureSampler, null);

			WasDestroyed = true;
		}

		[PublicAPI]
		public class Builder {
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

			private readonly VkDevice logicalDevice;

			public Builder(string debugName, VkDevice logicalDevice, VkFilter minFilter, VkFilter magFilter, float maxAnisotropy) {
				DebugName = debugName;
				this.logicalDevice = logicalDevice;
				MinFilter = minFilter;
				MagFilter = magFilter;
				MaxAnisotropy = maxAnisotropy;
			}

			public Builder(string debugName, VkDevice logicalDevice, VkFilter minFilter, VkFilter magFilter, VkPhysicalDeviceLimits physicalDeviceLimits) : this(debugName, logicalDevice, minFilter, magFilter,
				physicalDeviceLimits.maxSamplerAnisotropy) { }

			public void SetMipmapMode(VkSamplerMipmapMode mipmapMode, float mipLodBias, float minLod, float maxLod) {
				MipmapMode = mipmapMode;
				MipLodBias = mipLodBias;
				MinLod = minLod;
				MaxLod = maxLod;
			}

			[MustUseReturnValue]
			public VkTextureSamplerObject MakeTextureSampler() {
				VkSamplerCreateInfo samplerCreateInfo = new() {
						minFilter = MinFilter,
						magFilter = MagFilter,
						addressModeU = AddressMode.U,
						addressModeV = AddressMode.V,
						addressModeW = AddressMode.W,
						anisotropyEnable = (int)(AnisotropyEnable && Engine3.GameInstance.AllowEnableAnisotropy ? Vk.True : Vk.False),
						maxAnisotropy = MaxAnisotropy,
						borderColor = BorderColor,
						unnormalizedCoordinates = (int)(NormalizedCoordinates ? Vk.False : Vk.True),
						compareEnable = (int)Vk.False,
						compareOp = VkCompareOp.CompareOpAlways,
						mipmapMode = MipmapMode,
						mipLodBias = MipLodBias,
						minLod = MinLod,
						maxLod = MaxLod,
				};

				VkSampler textureSampler;
				VkH.CheckIfSuccess(Vk.CreateSampler(logicalDevice, &samplerCreateInfo, null, &textureSampler), VulkanException.Reason.CreateTextureSampler);
				return new(DebugName, logicalDevice, textureSampler);
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