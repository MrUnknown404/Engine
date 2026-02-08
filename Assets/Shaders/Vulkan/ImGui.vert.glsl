#version 460

layout (location = 0) in vec2 inPosition;
layout (location = 1) in vec2 inUVs;
layout (location = 2) in vec4 inColor;

layout (location = 0) out vec2 outUVs;
layout (location = 1) out vec4 outColor;

layout (push_constant, std430) uniform PushConsants {
	vec2 translate;
	vec2 scale;
};

layout (constant_id = 0) const bool UseFastLinearColorConversion = true;

vec4 toLinearSlow(vec4 sRGB) {
	bvec3 cutoff = lessThan(sRGB.rgb, vec3(0.04045));
	vec3 higher = pow((sRGB.rgb + vec3(0.055)) / vec3(1.055), vec3(2.4));
	vec3 lower = sRGB.rgb / vec3(12.92);
	return vec4(mix(higher, lower, cutoff), sRGB.a);
}

vec4 toLinearFast(vec4 sRGB) {
	return vec4(pow(sRGB.rgb, vec3(2.2)), sRGB.a);
}

void main() {
	gl_Position = vec4(inPosition * scale + translate, 0, 1);
	outUVs = inUVs;
	outColor = UseFastLinearColorConversion ? toLinearFast(inColor) : toLinearSlow(inColor);
}