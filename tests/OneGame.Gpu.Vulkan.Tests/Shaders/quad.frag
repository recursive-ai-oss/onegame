#version 450
layout(set = 0, binding = 0) uniform Params { vec4 tint; vec2 offset; } params;
layout(set = 0, binding = 1) uniform texture2D tex;
layout(set = 0, binding = 2) uniform sampler smp;
layout(location = 0) in vec2 vUv;
layout(location = 0) out vec4 outColor;
void main() { outColor = texture(sampler2D(tex, smp), vUv) * params.tint; }
