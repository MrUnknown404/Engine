#version 330

layout (location = 0) out vec4 OutColor;

uniform sampler2D Tex0;
uniform vec4 OutlineColor = vec4(0, 0, 0, 1);
uniform vec4 FontColor = vec4(0, 0, 0, 1);
uniform bool DrawOutline = true;
uniform bool DrawFont = true;
uniform int OutlineSize = 150; // 0-256

in vec2 F_Tex;

float gaussianBlur() {
	const int matrixSize = 15, kernelSize = (matrixSize - 1) / 2;
	const float offset = 0.001;

	float kernel[matrixSize];
	for (int j = 0; j <= kernelSize; j++) { kernel[kernelSize + j] = kernel[kernelSize - j] = 0.39894 * exp(-0.5 * j * j / 14.0) / 7.0; }

	float normalizationFactor = 0;
	for (int j = 0; j < matrixSize; j++) { normalizationFactor += kernel[j]; }
	normalizationFactor *= normalizationFactor;

	float outputColor = 0;
	for (int x = -kernelSize; x <= kernelSize; x++) {
		for (int y = -kernelSize; y <= kernelSize; y++) {
			outputColor += kernel[kernelSize + y] * kernel[kernelSize + x] * texture(Tex0, F_Tex + vec2(x * offset, y * offset)).r;
		}
	}

	return outputColor / (normalizationFactor * normalizationFactor);
}

void main() {
	float value = (256 - clamp(OutlineSize, 0, 256)) / 256.0;

	OutColor = vec4(0);
	if (DrawOutline) { OutColor += mix(mix(vec4(0), OutlineColor, smoothstep(0, value * 2, gaussianBlur())), OutlineColor, texture(Tex0, F_Tex).r); }
	if (DrawFont) { OutColor += mix(OutColor, DrawOutline ? FontColor - OutlineColor : FontColor, texture(Tex0, F_Tex).r); }
}