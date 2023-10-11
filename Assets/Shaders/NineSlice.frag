#version 330

layout (location = 0) out vec4 OutColor;

const float;

uniform sampler2D Tex0;
uniform vec4 Color = vec4(1);
uniform vec2 ObjectSize;

in vec2 F_Tex;

float map(float value, float originalMin, float originalMax, float newMin, float newMax) {
	return (value - originalMin) / (originalMax - originalMin) * (newMax - newMin) + newMin;
}

float processAxis(float coord, float textureBorder, float windowBorder) {
	if (coord < windowBorder) { return map(coord, 0, windowBorder, 0, textureBorder); }
	if (coord < 1 - windowBorder) {    return map(coord, windowBorder, 1 - windowBorder, textureBorder, 1 - textureBorder); }
	return map(coord, 1 - windowBorder, 1, 1 - textureBorder, 1);
}

void main(void) {
	float Slice = textureSize(Tex0, 0).x / 3.0;

	OutColor = texture2D(Tex0, vec2(
		processAxis(F_Tex.x, Slice / textureSize(Tex0, 0).x, Slice / ObjectSize.x),
		processAxis(F_Tex.y, Slice / textureSize(Tex0, 0).y, Slice / ObjectSize.y)
	)) * Color;
}