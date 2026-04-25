import viewProj, vs_transformPosition from ChaosGraphics.Instancing;
import ChaosGraphics.Shade;

vec4 shade(
	vec3 worldPos,
	vec3 normal,
	vec4 diffuse,
	vec4 specular,
	vec4 position,
	vec4 color,
	vec4 ambientRange
) {
	float distance = length(worldPos - position.xyz);
	vec3 dir = (worldPos - position.xyz) / distance;
	vec4 result = shadeDefault(diffuse, specular, worldPos, normal, dir, max(0, position.w - distance) / position.w) * color;
	result +=
		diffuse
		* max(0.0, ambientRange.x * position.w - distance) / (ambientRange.x * position.w)
		* color
		* ambientRange.y;

	return result;
}

void fs_Simple(
	vec4 position : POSITION_RANGE,
	vec4 color : LIGHTCOLOR,
	vec4 ambientRange: AMBIENT,
	out vec4 result : COLOR0
) {
	ivec2 tex = ivec2(gl_FragCoord.xy);
 	vec3 worldPos = texelFetch(positionSampler, tex, 0).xyz;
	vec3 normal;
	vec4 diffuse, specular;
	sampleLightData(tex, normal, diffuse, specular);

	result = shade(worldPos, normal, diffuse, specular, position, color, ambientRange);
}

void vs_passInstanceParams(
	vec4 inPosRange : INSTANCE_POSITION_RANGE,
	vec4 inColor : INSTANCE_COLOR,
	vec4 inAmbient : INSTANCE_AMBIENT,
	out vec4 outPosRange : POSITION_RANGE,
	out vec4 outColor : LIGHTCOLOR,
	out vec4 outAmbient : AMBIENT
) {
	outPosRange = inPosRange;
	outColor = inColor;
	outAmbient = inAmbient;
}

void vs_avoidClipping() {
	gl_Position.z = 0.5;
}

Pass Instanced {
	CullFace(Back);
	Enable(DepthTest, true);
	VertexShader = vs_transformPosition, vs_passInstanceParams;
	FragmentShader = fs_Simple;
}

Pass InstancedBackface {
	CullFace(Front);
	Enable(DepthTest, false);
	VertexShader = vs_transformPosition, vs_passInstanceParams, vs_avoidClipping;
	FragmentShader = fs_Simple;
}