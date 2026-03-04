# TODO

I'll remove the entry when done

---

## Engine
- redo logging
  - add way more debug logging
  - add more levels/log types
  - multiple files or single file?
  - NLog doesn't print generic type names correctly. either fix or use something else
- make code analyzer for minimum params count
- code generator for making Vertex structs easier?
- document way more
- add multithreading 

---

## Graphics

### Vulkan
- read https://github.com/KhronosGroup/Vulkan-ValidationLayers/blob/main/docs/debug_printf.md
- read https://docs.vulkan.org/samples/latest/samples/extensions/descriptor_indexing/README.html
- read https://docs.vulkan.org/guide/latest/buffer_device_address.html
- read https://github.com/SaschaWillems/Vulkan/blob/master/examples/indirectdraw/README.md#preparing-the-indirect-draw
- fix white screen while resizing. is it possible to show the last swap image and scale then present?
- what's the best way of swapping images? do i use vkUpdateDescriptorSets or bind a different set? or something else?
- use stackalloc more when appropriate

### OpenGL

## ImGui
- setup ImPlot