import ChaosGraphics.SpotLight;

#define PASS_INSTANCE_PARAMS PASS(vec4 POSITION_RANGE), PASS(vec4 LIGHT_COLOR), PASS(vec4 DIRECTION_ANGLE), PASS(vec4 FALLOFF)

float getMaskSample(
	vec4 directionAngle,
	vec4 falloff,
	vec3 dir
) {
	float angleDelta = acos(dot(directionAngle.xyz, dir));
	return clamp((1.0 - clamp((angleDelta / directionAngle.w), 0.0, 1.0)) / falloff.x, 0.0, 1.0);
}

vec4 shade_base(
	vec3 worldPos,
	vec3 normal,
	vec4 diffuse,
	vec4 specular,
	vec4 position,
	vec4 color,
	vec3 direction,
	out vec3 lightToFragmentDirection
) {
	float distance = length(worldPos - position.xyz);
	lightToFragmentDirection = (worldPos - position.xyz) / distance;

	float d = max(0, -dot(lightToFragmentDirection, normal) * max(0.0, (1.0 - distance / position.w)));
	vec3 reflection = lightToFragmentDirection - dot(normal, lightToFragmentDirection) * normal * 2.0;
	vec3 eye = normalize(cameraPosition.xyz - worldPos);
	float sharpness = specular.a;
	float s = min(d, clamp(pow(max(0.0, dot(eye, reflection)), sharpness), 0.0, 1.0));

	return max(vec4(0.0), (d * diffuse + s * specular) * color);
}

vec4 shade(
	vec3 worldPos,
	vec3 normal,
	vec4 diffuse,
	vec4 specular,
	vec4 position,
	vec4 color,
	vec4 directionAngle,
	vec4 falloff
) {
	vec3 lightToFragmentDirection;
	vec4 base = shade_base(worldPos, normal, diffuse, specular, position, color, directionAngle.xyz, lightToFragmentDirection);
	return base * getMaskSample(directionAngle, falloff, lightToFragmentDirection);
}

void fs_Mask(
	vec4 directionAngle : DIRECTION_ANGLE,
	vec4 falloff : FALLOFF,
	vec3 dir : LIGHT_TO_FRAGMENT_DIRECTION,
	vec3 _ : WORLD_POS
) {
	float angleDelta = acos(dot(directionAngle.xyz, dir));
	COLOR0 *= getMaskSample(directionAngle, falloff, dir);
}
