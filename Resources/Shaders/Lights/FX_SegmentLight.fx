import viewProj from ChaosGraphics.Instancing;
import ChaosGraphics.Shade;

vec4 viewport : SCREEN_SIZE;

#define PASS_INSTANCE_PARAMS PASS(vec4 POSITION_RANGE_1), PASS(vec4 LIGHT_COLOR_1), PASS(vec4 POSITION_RANGE_2), PASS(vec4 LIGHT_COLOR_2)

#define EPSILON 0.01234

vec4 shade(
	vec3 worldPos,
	vec3 normal,
	vec4 diffuse,
	vec4 specular,
	vec4 positionRange1,
	vec4 color1,
	vec4 positionRange2,
	vec4 color2
) {
	// Interpolate points
	vec4 pe = positionRange2 - positionRange1;
	float segmentLength = length(pe.xyz);
	float ratio = pe.w / segmentLength;

	float segment_f = dot(worldPos - positionRange1.xyz, pe.xyz) / dot(pe.xyz, pe.xyz);
	vec4 projection = positionRange1 + segment_f * pe;

	vec3 delta = projection.xyz - worldPos; // TODO: try to compute this without knowing 'projection'
	segment_f = clamp(segment_f + length(delta) * ratio / segmentLength, 0.0, 1.0);
	projection = positionRange1 + segment_f * pe;
	vec4 lightColor = color1 + segment_f * (color2 - color1);

	float distance = length(worldPos - projection.xyz);
	vec3 dir = (worldPos - projection.xyz) / distance;
	return shadeDefault(diffuse, specular, worldPos, normal, dir, max(0, projection.w - distance) / projection.w) * lightColor;
}

void fs_Simple(
	vec4 positionRange1 : POSITION_RANGE_1,
	vec4 color1 : LIGHT_COLOR_1,
	vec4 positionRange2 : POSITION_RANGE_2,
	vec4 color2 : LIGHT_COLOR_2,
	out vec4 result : COLOR0
) {
	ivec2 tex = ivec2(gl_FragCoord.xy);
	vec3 worldPos = texelFetch(positionSampler, tex, 0).xyz;
	vec3 normal;
	vec4 diffuse, specular;
	sampleLightData(tex, normal, diffuse, specular);

	result = shade(worldPos, normal, diffuse, specular, positionRange1, color1, positionRange2, color2);
}

void vs_SegmentLight(
	vec4 positionRange1 : POSITION_RANGE_1,
	vec4 color1 : LIGHT_COLOR_1,
	vec4 positionRange2 : POSITION_RANGE_2,
	vec4 color2 : LIGHT_COLOR_2,
	out vec4 position : gl_Position
) {
	float aspectRatio = (viewport.z - viewport.x) / (viewport.w - viewport.y);

	vec4 projected1 = vec4(positionRange1.xyz, 1.0) * viewProj;
	vec4 projected2 = vec4(positionRange2.xyz, 1.0) * viewProj;

	float r1 = (positionRange1.w / projected1.w) * 2; // nobody knows why * 2 ...
	float r2 = (positionRange2.w / projected2.w) * 2;

	vec2 c1 = projected1.xy / projected1.w;
	vec2 c2 = projected2.xy / projected2.w;

	// Undo the aspect ratio from the projection matrix
	c1.x *= aspectRatio;
	c2.x *= aspectRatio;

	vec2 dir = c1 - c2;
	vec2 ortho = normalize(vec2(-dir.y, dir.x));

	vec2 c = gl_VertexID < 2 ? c1 : c2;

	float orthoSign = gl_VertexID % 2 == 0 ? -1.0 : 1.0;
	float alongSign = gl_VertexID < 2 ? -1.0 : 1.0;

	float radDiff = r1 - r2;
	if (abs(radDiff) < EPSILON)
	{
		float r = max(r1, r2);
		vec2 z = c - dir * r * alongSign;
		position.xy = z + orthoSign * ortho * r;
	}
	else
	{
		vec2 t = (c2 * r1 - c1 * r2) / radDiff;
		float swapAlong = sign(radDiff) * alongSign;

		vec2 h = t - c;
		float len = length(h);

		float r = gl_VertexID < 2 ? r1 : r2;
		float radRatio = r / sqrt(len * len - r * r);
		float g = (len - r * swapAlong) / len;
		vec2 z = t - h * g;
		vec2 n = ortho * length(z - t) * radRatio;

		position.xy = z + n * orthoSign;
	}

	// Reapply the aspect ratio
	position.x /= aspectRatio;

	position.z = 0.5;
	position.w = 1.0;
}

// TODO: fix this pass and add a pass for lights covering the entire screen / with points behind the camera
Pass SegmentLight
{
	Enable(CullFace, false);
	Enable(DepthTest, false);
	VertexShader = vs_SegmentLight, PASS_INSTANCE_PARAMS;
	FragmentShader = fs_Simple;
}
