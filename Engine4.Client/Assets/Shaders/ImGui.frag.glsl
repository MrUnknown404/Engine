#version 460

layout (location = 0) in vec2 inUVs;
layout (location = 1) in vec4 inColor;

layout (location = 0) out vec4 outColor;

layout (set = 0, binding = 0) uniform sampler _sampler;
layout (set = 1, binding = 0) uniform texture2D _texture;

void main() {
	outColor = texture(sampler2D(_texture, _sampler), inUVs) * inColor;
}