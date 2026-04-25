import viewProj, vs_transformPosition from ChaosGraphics.Instancing;
import ChaosGraphics.Shade;

#define PASS_INSTANCE_PARAMS PASS(vec4 POSITION_RANGE), PASS(vec4 LIGHT_COLOR), PASS(vec4 DIRECTION_FALLOFF), PASS(vec4 ANGLE)

vec4 shade(
	vec3 worldPos,
	vec3 normal,
	vec4 diffuse,
	vec4 specular,
	vec4 position,
	vec4 color,
	vec4 directionFalloff,
	vec4 angle
) {
	float distance = length(worldPos - position.xyz);
	vec3 dir = (worldPos - position.xyz) / distance;
	float angleDelta = acos(dot(directionFalloff.xyz, dir));
	float falloff = clamp((1.0 - clamp((angleDelta / angle.a), 0.0, 1.0)) / directionFalloff.w, 0.0, 1.0);

	float d = max(0, -dot(dir, normal) * max(0.0, (1.0 - distance / position.w)));
	vec3 reflection = dir - dot(normal, dir) * normal * 2.0;
	vec3 eye = normalize(cameraPosition.xyz - worldPos);
	float sharpness = specular.a;
	float s = min(d, clamp(pow(max(0.0, dot(eye, reflection)), sharpness), 0.0, 1.0));
	return max(vec4(0.0), (d * diffuse + s * specular) * color * falloff);
}

void fs_Simple(
	vec4 position : POSITION_RANGE,
	vec4 color : LIGHT_COLOR,
	vec4 directionFalloff : DIRECTION_FALLOFF,
	vec4 angle : ANGLE,
	out vec4 result : COLOR0
) {
	ivec2 tex = ivec2(gl_FragCoord.xy);
	vec3 worldPos = texelFetch(positionSampler, tex, 0).xyz;
	vec3 normal;
	vec4 diffuse, specular;
	sampleLightData(tex, normal, diffuse, specular);

	result = shade(worldPos, normal, diffuse, specular, position, color, directionFalloff, angle);
}

void vs_avoidClipping() {
	gl_Position.z = 0.5;
}

Pass Instanced{
	CullFace(Back);
	Enable(DepthTest, true);
	VertexShader = vs_transformPosition, PASS_INSTANCE_PARAMS;
	FragmentShader = fs_Simple;
}

Pass InstancedBackface{
	CullFace(Front);
	Enable(DepthTest, false);
	VertexShader = vs_transformPosition, PASS_INSTANCE_PARAMS, vs_avoidClipping;
	FragmentShader = fs_Simple;
}