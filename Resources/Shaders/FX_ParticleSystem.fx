import vs_transformPosition from ChaosGraphics.Sprite;
import texCoords, positions, transform, viewProj from ChaosGraphics.Sprite;

vec2 texAtlasSize = vec2(1.0, 1.0);

void vs_createParticleInstanced(
	mat4 instanceTransform : INSTANCE_TRANSFORM,
	vec4 instanceTexOffset : PARTICLE_TEXOFFSET,
	vec4 instanceColor : PARTICLE_COLOR,
	out vec4 position : gl_Position,
	out vec2 texCoord : TEXCOORD0,
	out vec4 outColor : COLOR0
) {
	outColor = instanceColor;
	texCoord = (texCoords[gl_VertexID] + instanceTexOffset.xy) * texAtlasSize;
	vs_transformPosition((vec4(positions[gl_VertexID], 0.0, 1.0) * instanceTransform).xyz, position);
}
