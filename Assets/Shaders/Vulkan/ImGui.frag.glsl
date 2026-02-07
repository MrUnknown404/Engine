#version 460

layout (location = 0) in vec2 inUVs;
layout (location = 1) in vec4 inColor;

layout (location = 0) out vec4 outColor;

layout (binding = 0) uniform sampler2D texSampler;

void main() {
	outColor = texture(texSampler, inUVs) * inColor;
}