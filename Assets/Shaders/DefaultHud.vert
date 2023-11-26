#version 330

layout (location = 0) in vec3 Pos;
layout (location = 1) in vec2 Tex;

uniform mat4 Projection;
uniform vec3 Position = vec3(0);
uniform vec3 Scale = vec3(1);

out vec2 F_Tex;

void main(void) {
	gl_Position = vec4(Pos + Position, 1) * vec4(Scale, 1) * Projection;
	F_Tex = Tex;
}