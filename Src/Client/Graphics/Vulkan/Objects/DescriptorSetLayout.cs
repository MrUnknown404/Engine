using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public sealed unsafe class DescriptorSetLayout : GraphicsResource<DescriptorSetLayout, ulong> {
		public VkDescriptorSetLayout VkDescriptorSetLayout { get; }

		protected override ulong Handle => VkDescriptorSetLayout.Handle;

		private readonly LogicalGpu logicalGpu;

		internal DescriptorSetLayout(LogicalGpu logicalGpu, DescriptorSetInfo[] descriptorSets) {
			this.logicalGpu = logicalGpu;
			VkDescriptorSetLayout = CreateDescriptorSetLayout(logicalGpu, descriptorSets);

			PrintCreate();
		}

		protected override void Cleanup() => Vk.DestroyDescriptorSetLayout(logicalGpu.LogicalDevice, VkDescriptorSetLayout, null);

		[MustUseReturnValue]
		private static VkDescriptorSetLayout CreateDescriptorSetLayout(LogicalGpu logicalGpu, DescriptorSetInfo[] descriptorSets) {
			VkDescriptorSetLayoutBinding[] bindings = descriptorSets.Select(static info => new VkDescriptorSetLayoutBinding {
					binding = info.BindingLocation, descriptorType = info.DescriptorType, stageFlags = info.StageFlags, descriptorCount = 1,
			}).ToArray();

			fixed (VkDescriptorSetLayoutBinding* bindingsPtr = bindings) {
				VkDescriptorSetLayoutCreateInfo layoutCreateInfo = new() { bindingCount = (uint)bindings.Length, pBindings = bindingsPtr, };
				VkDescriptorSetLayout descriptorSetLayout;
				VkH.CheckIfSuccess(Vk.CreateDescriptorSetLayout(logicalGpu.LogicalDevice, &layoutCreateInfo, null, &descriptorSetLayout), VulkanException.Reason.CreateDescriptorSetLayout);
				return descriptorSetLayout;
			}
		}
	}
}