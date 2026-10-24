#version 450
// Position and texture coordinate come from a vertex buffer; a uniform block offsets the quad.
layout(location = 0) in vec2 inPos;
layout(location = 1) in vec2 inUv;
layout(set = 0, binding = 0) uniform Params { vec4 tint; vec2 offset; } params;
layout(location = 0) out vec2 vUv;
void main() {
    gl_Position = vec4(inPos + params.offset, 0.0, 1.0);
    vUv = inUv;
}
