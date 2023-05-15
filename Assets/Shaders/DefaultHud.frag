#version 330

uniform sampler2D Tex0;
uniform vec4 Color = vec4(1);

in vec2 F_Tex;

void main() {
	gl_FragColor = texture(Tex0, F_Tex) * Color;
}