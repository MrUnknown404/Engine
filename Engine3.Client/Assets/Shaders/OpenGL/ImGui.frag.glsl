#version 460 core

in vec2 fragUVs;
in vec4 fragColor;

out vec4 outColor;

uniform sampler2D tex0;

void main() {
	outColor = texture(tex0, fragUVs) * fragColor;
}