import transform, viewProj from ChaosGraphics.VertexShaders;
import vs_transformPosition from ChaosGraphics.VertexShaders;

sampler2D fontTex : TEX;
sampler2D sdfTex : SDF;
vec4 color = vec4(1,1,1,1);
vec4 channelMultipliers = vec4(0, 1, 0, 0);
vec4 sdfParams = vec4(0.5, 2.117, 1, 0);

vec4 applySdf(vec4 c, vec2 inTex, float stretch) {
	float sdfAlpha = 1 - texture(sdfTex, inTex).r;
	float effectiveSmooth = sdfParams.y / (1 + stretch * 79.133);
	sdfAlpha = smoothstep(sdfParams.x - effectiveSmooth, sdfParams.x + effectiveSmooth, sdfAlpha);
	return vec4(c.rgb, c.a * sdfAlpha);
}

void fs_Text_Material(
	vec4 inTex : TEXCOORD0,
	float stretch : TEXCOORD1,
	out vec4 emissive : COLOR0,
	out vec4 diffuse : COLOR1,
	out vec4 specular : COLOR2)
{
	vec4 sdfSample = applySdf(texture(fontTex, inTex.zw) * color, inTex.xy, stretch);
	emissive = vec4(sdfSample.rgb, sdfSample.a * channelMultipliers[0]);
	diffuse = vec4(sdfSample.rgb, sdfSample.a * channelMultipliers[1]);
	specular = vec4(sdfSample.rgb, sdfSample.a * channelMultipliers[2]);
}

void fs_Text_MaterialColored(
	vec4 inCol : COLOR0,
	vec4 inTex : TEXCOORD0,
	float stretch : TEXCOORD1,
	out vec4 emissive : COLOR0,
	out vec4 diffuse : COLOR1,
	out vec4 specular : COLOR2)
{
	vec4 sdfSample = applySdf(texture(fontTex, inTex.zw) * color, inTex.xy, stretch) * inCol;
	emissive = vec4(sdfSample.rgb, sdfSample.a * channelMultipliers[0]);
	diffuse = vec4(sdfSample.rgb, sdfSample.a * channelMultipliers[1]);
	specular = vec4(sdfSample.rgb, sdfSample.a * channelMultipliers[2]);
}

void fs_Text_HUD(vec4 inTex : TEXCOORD0, float stretch : TEXCOORD1, out vec4 outCol : COLOR0)
{
	outCol = applySdf(texture(fontTex, inTex.zw) * color, inTex.xy, stretch);
}

void fs_Text_HUDColored(vec4 inTex : TEXCOORD0, float stretch : TEXCOORD1, vec4 inColor : COLOR0, out vec4 outCol : COLOR0)
{
	outCol = applySdf(texture(fontTex, inTex.zw) * color * inColor, inTex.xy, stretch);
}

void fs_Selection(out vec4 outCol : COLOR0) {
	outCol = color;
}

void fs_Text_World(
	vec3 inWorldPos : TEXCOORD2,
	vec4 inTex : TEXCOORD0,
	float stretch : TEXCOORD1,
	out vec4 outPosition : COLOR0,
	out vec4 outNormal : COLOR1,
	out vec4 outSurface : COLOR2)
{
	vec4 col = applySdf(texture(fontTex, inTex.zw), inTex.xy, stretch);
	if (col.a < 0.5) discard;
	outPosition = vec4(inWorldPos.xyz, 1.0);
	outNormal = vec4(-viewProj[2].xyz, 1.0);
	outSurface = vec4(1.0);
}

void vs_transformPosTexStretch(
	vec3 inPosition : POSITION,
	vec4 inTex : TEXCOORD,
	out vec4 outPosition : gl_Position,
	out vec4 outTexCoord : TEXCOORD0,
	out float stretch : TEXCOORD1)
{
	mat4 worldViewProj = transform * viewProj;
	outPosition = vec4(inPosition.xy, 0.0, 1.0) * worldViewProj;
	vec4 s = vec4(inPosition.x + inPosition.z, inPosition.y + inPosition.z, 0.0, 1.0) * worldViewProj;
	stretch = length(outPosition.xy / outPosition.z - s.xy / s.z) / 1.41;
	outTexCoord = inTex;
}

void vs_transformPosWorldTex(
	vec3 inPosition : POSITION,
	vec4 inTex : TEXCOORD,
	out vec4 outPosition : gl_Position,
	out vec3 outWorldPos : TEXCOORD2,
	out vec4 outTex : TEXCOORD0,
	out float stretch : TEXCOORD1)
{
	outWorldPos = (vec4(inPosition.xy, 0.0, 1.0) * transform).xyz;
	mat4 worldViewProj = transform * viewProj;
	outPosition = vec4(inPosition.xy, 0.0, 1.0) * worldViewProj;
	vec4 s = vec4(inPosition.x + inPosition.z, inPosition.y + inPosition.z, 0.0, 1.0) * worldViewProj;
	stretch = length(outPosition.xy / outPosition.z - s.xy / s.z) / 1.41;
	outTex = inTex;
}

Pass HUD {
	Enable(Blend, true);
	BlendFuncSeperate(SrcAlpha, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	Enable(CullFace, false);
	Enable(DepthTest, false);
	VertexShader = vs_transformPosTexStretch;
	FragmentShader = fs_Text_HUD;
}

Pass HUDColored {
	Enable(Blend, true);
	BlendFuncSeperate(SrcAlpha, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	Enable(CullFace, false);
	Enable(DepthTest, false);
	VertexShader = vs_transformPosTexStretch, PASS(vec4 COLOR0);
	FragmentShader = fs_Text_HUDColored;
}

Pass Selection {
	Enable(Blend, true);
	BlendFuncSeperate(SrcAlpha, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	Enable(CullFace, false);
	Enable(DepthTest, false);
	VertexShader = vs_transformPosition;
	FragmentShader = fs_Selection;
}

Pass Material {
	Enable(Blend, true);
	BlendFuncSeperate(SrcAlpha, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	Enable(CullFace, false);
	Enable(DepthTest, true);
	DepthFunc(Lequal);
	DepthMask(false);
	VertexShader = vs_transformPosTexStretch;
	FragmentShader = fs_Text_Material;
}

Pass MaterialColored {
	Enable(Blend, true);
	BlendFuncSeperate(SrcAlpha, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	Enable(CullFace, false);
	Enable(DepthTest, true);
	DepthFunc(Lequal);
	DepthMask(false);
	VertexShader = vs_transformPosTexStretch, PASS(vec4 COLOR0);
	FragmentShader = fs_Text_MaterialColored;
}

Pass World {
	Enable(CullFace, false);
	Enable(DepthTest, true);
	DepthMask(true);
	VertexShader = vs_transformPosWorldTex;
	FragmentShader = fs_Text_World;
}
