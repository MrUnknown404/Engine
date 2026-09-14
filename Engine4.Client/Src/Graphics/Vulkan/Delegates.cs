using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Graphics.Vulkan;

public delegate bool IsPhysicalDeviceSuitableDelegate(PhysicalGpuProperties physicalGpuProperties);
public delegate BoundPhysicalGpu SelectGpuDelegate(BoundPhysicalGpu[] physicalGpus);
public delegate int RateGpuSuitabilityDelegate(BoundPhysicalGpu physicalGpu);