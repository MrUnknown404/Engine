#version 330

layout (location = 0) out vec4 OutColor;

uniform sampler2D Tex0;
uniform vec4 Color = vec4(1);

in vec2 F_Tex;

void main() {
	OutColor = texture(Tex0, F_Tex) * Color;
}