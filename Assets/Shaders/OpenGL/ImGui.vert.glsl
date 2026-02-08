#version 460 core

// ARB_separate_shader_objects requires this to be declared
out gl_PerVertex {
	vec4 gl_Position;
	float gl_PointSize;
	float gl_ClipDistance[];
};

struct VertexData {
	float position[2];
	float uv[2];
	uint color;
};

layout (binding = 0, std430) readonly buffer VertexBuffer {
	VertexData vertices[];
};

layout (binding = 1, std430) readonly buffer IndexBuffer {
	uint indices[];
};

uniform vec2 translate;
uniform vec2 scale;

out vec2 fragUVs;
out vec4 fragColor;

vec2 getPosition() {
	VertexData vertex = vertices[indices[gl_VertexID]];
	return vec2(vertex.position[0], vertex.position[1]);
}

vec2 getUVs() {
	VertexData vertex = vertices[indices[gl_VertexID]];
	return vec2(vertex.uv[0], vertex.uv[1]);
}

// Because i'm using vertex pulling i need to manually convert packed color into a vec4
vec4 getColor() {
	const float s = 1.0 / 255.0;

	VertexData vertex = vertices[indices[gl_VertexID]];

	float r = ((vertex.color >> 0) & 0xFF) * s;
	float g = ((vertex.color >> 8) & 0xFF) * s;
	float b = ((vertex.color >> 16) & 0xFF) * s;
	float a = ((vertex.color >> 24) & 0xFF) * s;

	return vec4(r, g, b, a);
}

void main() {
	gl_Position = vec4(getPosition() * scale + translate, 0, 1);
	fragUVs = getUVs();
	fragColor = getColor();
}