using Engine3.Exceptions;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public sealed unsafe class DescriptorSetLayout : GraphicsResource<DescriptorSetLayout, ulong> {
		public VkDescriptorSetLayout VkDescriptorSetLayout { get; }

		protected override ulong Handle => VkDescriptorSetLayout.Handle;

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

			PrintCreate();
		}

		protected override void Cleanup() => Vk.DestroyDescriptorSetLayout(logicalDevice, VkDescriptorSetLayout, null);
	}
}