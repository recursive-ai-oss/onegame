#version 450
// Three hard-coded vertices with red, green and blue corners.
layout(location = 0) out vec3 vColor;
void main() {
    vec2 pos[3] = vec2[](vec2(-0.8, -0.8), vec2(0.8, -0.8), vec2(0.0, 0.8));
    vec3 col[3] = vec3[](vec3(1, 0, 0), vec3(0, 1, 0), vec3(0, 0, 1));
    gl_Position = vec4(pos[gl_VertexIndex], 0.0, 1.0);
    vColor = col[gl_VertexIndex];
}
