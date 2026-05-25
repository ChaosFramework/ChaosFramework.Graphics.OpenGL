import vs_fullscreen from ChaosGraphics.VertexShaders;

const ivec2 offsets[9] = ivec2[] (
	ivec2(-1, -1),
	ivec2( 0, -1),
	ivec2( 1, -1),
	ivec2(-1,  0),
	ivec2( 0,  0),
	ivec2( 1,  0),
	ivec2(-1,  1),
	ivec2( 0,  1),
	ivec2( 1,  1)
);

sampler2D renderResultSampler;
sampler2D historySampler;
sampler2D depthSampler;
vec4 pixelOffset;
mat4 reProjection;
float blendFactor;
float historyClampFactor;
float weights[9];
vec4 viewport;

sampler2D positionSampler;
vec2 halfsizeViewPlane;
float zNear, zFar;
mat4 invPersMatrix;

vec4 clip_aabb(vec3 aabb_min, vec3 aabb_max, vec4 p, vec4 q)
{
	vec3 p_clip = 0.5 * (aabb_max + aabb_min);
	vec3 e_clip = 0.5 * (aabb_max - aabb_min);
	vec4 v_clip= q - vec4(p_clip, p.w);
	vec3 v_unit= v_clip.xyz / e_clip;
	vec3 a_unit= abs(v_unit);
	float ma_unit = max(a_unit.x, max(a_unit.y,a_unit.z));
	if (ma_unit > 1.0)
		return vec4(p_clip, p.w) + v_clip / ma_unit;
	else
		return q; // point inside aabb
}

vec4 CalcEyeFromNDC(float z_b, vec3 eyeDirection) {
    float z_n = 2.0 * z_b - 1.0;
    float z_e = 2.0 * zNear * zFar / (zFar + zNear - z_n * (zFar - zNear));
	return vec4(eyeDirection * z_e, 1);
}

void fs_sample(vec2 texCoord : TEXCOORD0, out vec4 result : COLOR0)
{
	ivec2 xy = ivec2(gl_FragCoord.xy);
	float depth = texelFetch(depthSampler, xy, 0).r;
	vec4 viewPos = texelFetch(positionSampler, xy, 0);
	vec4 oldProjection = vec4(viewPos.xyz, 1.0) * reProjection;
	vec2 oldTexCoord = 0.5 + ((oldProjection.xy / oldProjection.w) * 0.5);

	vec4 newColor = vec4(0);

	vec3 colorBoxMax = vec3(-1.0 / 0.0000001); // -infinity
	vec3 colorBoxMin = vec3( 1.0 / 0.0000001); // +infinity

	for (int i = 0; i < 9; ++i) {
		vec4 there = texelFetch(renderResultSampler, xy + offsets[i], 0);
		newColor += there * weights[i];
		colorBoxMax = max(there.xyz, colorBoxMax);
		colorBoxMin = min(there.xyz, colorBoxMin);
	}

	vec4 history = texture(historySampler, oldTexCoord);
	history += (clip_aabb(colorBoxMin, colorBoxMax, newColor, history) - history) * historyClampFactor;

	if (any(notEqual(oldTexCoord, clamp(oldTexCoord, 0.f, 1.f))))
		history = newColor;
	result = newColor + (history - newColor) * blendFactor;
}

Pass TAA {
	Enable(Blend, false);
	Enable(DepthTest, false);
	Enable(CullFace, false);
	VertexShader = vs_fullscreen;
	FragmentShader = fs_sample;
}
