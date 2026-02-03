using Engine3.Exceptions;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class DescriptorSetLayout : IGraphicsResource, IEquatable<DescriptorSetLayout> {
		public VkDescriptorSetLayout VkDescriptorSetLayout { get; }

		public bool WasDestroyed { get; private set; }
		private readonly VkDevice logicalDevice;

		internal DescriptorSetLayout(VkDevice logicalDevice, DescriptorSetInfo[] descriptorSets) {
			this.logicalDevice = logicalDevice;

			VkDescriptorSetLayoutBinding[] bindings = descriptorSets.Select(static info => new VkDescriptorSetLayoutBinding {
					binding = info.BindingLocation, descriptorType = info.DescriptorType, stageFlags = info.StageFlags, descriptorCount = 1,
			}).ToArray();

			fixed (VkDescriptorSetLayoutBinding* bindingsPtr = bindings) {
				VkDescriptorSetLayoutCreateInfo layoutCreateInfo = new() { bindingCount = (uint)bindings.Length, pBindings = bindingsPtr, };
				VkDescriptorSetLayout descriptorSetLayout;
				VkH.CheckIfSuccess(Vk.CreateDescriptorSetLayout(logicalDevice, &layoutCreateInfo, null, &descriptorSetLayout), VulkanException.Reason.CreateDescriptorSetLayout);
				VkDescriptorSetLayout = descriptorSetLayout;
			}

			IGraphicsResource.PrintNameWithHandle<DescriptorSetLayout>(VkDescriptorSetLayout.Handle);
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			Vk.DestroyDescriptorSetLayout(logicalDevice, VkDescriptorSetLayout, null);

			WasDestroyed = true;
		}

		public bool Equals(DescriptorSetLayout? other) => other != null && VkDescriptorSetLayout == other.VkDescriptorSetLayout;
		public override bool Equals(object? obj) => obj is DescriptorSetLayout descriptorSetLayout && Equals(descriptorSetLayout);

		public override int GetHashCode() => VkDescriptorSetLayout.GetHashCode();

		public static bool operator ==(DescriptorSetLayout? left, DescriptorSetLayout? right) => Equals(left, right);
		public static bool operator !=(DescriptorSetLayout? left, DescriptorSetLayout? right) => !Equals(left, right);
	}
}