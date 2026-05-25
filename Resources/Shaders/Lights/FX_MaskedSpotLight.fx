import ChaosGraphics.SpotLight;

#define PASS_INSTANCE_PARAMS PASS(vec4 POSITION_RANGE), PASS(vec4 LIGHT_COLOR), PASS(vec4 DIRECTION_ANGLE), vs_InvertInstanceTransform

sampler2D mask;

vec4 getMaskSample(
	vec3 worldPos,
	float angle,
	mat4 invLightTransform
) {
	vec3 worldPosCameraSpace = (vec4(worldPos, 1.0f) * invLightTransform).xyz;
	float angleMultiplier = worldPosCameraSpace.z * tan(angle);
	worldPosCameraSpace.xy /= angleMultiplier;
	if (abs(worldPosCameraSpace.x) > 1.0f || abs(worldPosCameraSpace.y) > 1.0f)
		discard;

	return texture(mask, (worldPosCameraSpace.xy + 1.0f) / 2.0f);
}

void fs_Mask(
	vec3 worldPos : WORLD_POS,
	vec4 directionAngle : DIRECTION_ANGLE,
	mat4 invLightTransform : INV_INSTANCE_TRANSFORM,
	vec3 _ : LIGHT_TO_FRAGMENT_DIRECTION
) {
	COLOR0 *= getMaskSample(worldPos, directionAngle.w, invLightTransform);
}

void vs_InvertInstanceTransform(
	mat4 instanceTransform : INSTANCE_TRANSFORM,
	out mat4 invInstanceTransform : INV_INSTANCE_TRANSFORM
) {
	invInstanceTransform = inverse(instanceTransform);
}
