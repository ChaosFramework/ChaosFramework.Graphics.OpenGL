import transform, viewProj from ChaosGraphics.VertexShaders;

sampler2DArray fontTex : TEX;
sampler2DArray sdfTex : SDF;
vec4 textureBounds[256];
vec4 maxBounds;
int numMetaPerRow;

sampler2D metaSampler;

vec4 sdfParams = vec4(0.5, 2.117, 1, 0);

vec4 applySdf(vec4 color, vec3 inTex, float stretch) {
	float sdfAlpha = 1 - texture(sdfTex, inTex).r;
	float effectiveSmooth = sdfParams.y / (1 + stretch * 79.133);
	sdfAlpha = smoothstep(sdfParams.x - effectiveSmooth, sdfParams.x + effectiveSmooth, sdfAlpha);
	return vec4(color.rgb, color.a * sdfAlpha);
}

void fs_Text_MaterialColored(
	vec4 inCol : COLOR0,
	vec4 inTex : TEXCOORD0,
	float stretch : TEXCOORD1,
	vec4 fontIndexChannelMult : FONT_INDEX_CHANNEL_MULT,
	out vec4 emissive : COLOR0,
	out vec4 diffuse : COLOR1,
	out vec4 specular : COLOR2)
{
	vec4 sdfSample = applySdf(
		texture(fontTex, vec3(inTex.zw, fontIndexChannelMult[3])) * inCol,
		vec3(inTex.xy, fontIndexChannelMult[3]),
		stretch
		) * inCol;

	emissive = vec4(sdfSample.rgb, sdfSample.a * fontIndexChannelMult[0]);
	diffuse = vec4(sdfSample.rgb, sdfSample.a * fontIndexChannelMult[1]);
	specular = vec4(sdfSample.rgb, sdfSample.a * fontIndexChannelMult[2]);
}

void fs_Text_HUDColored(
	vec4 inTex : TEXCOORD0,
	float stretch : TEXCOORD1,
	vec4 fontIndexChannelMult : FONT_INDEX_CHANNEL_MULT,
	vec4 inColor : COLOR0,
	out vec4 outCol : COLOR0)
{
	outCol = applySdf(
		texture(fontTex, vec3(inTex.zw, fontIndexChannelMult[3])) * inColor,
		vec3(inTex.xy, fontIndexChannelMult[3]),
		stretch
		);
}

void fs_Text_World(
	vec3 inWorldPos : TEXCOORD2,
	vec4 inTex : TEXCOORD0,
	float stretch : TEXCOORD1,
	vec4 fontIndexChannelMult : FONT_INDEX_CHANNEL_MULT,
	out vec4 outPosition : COLOR0,
	out vec4 outNormal : COLOR1,
	out vec4 outSurface : COLOR2)
{
	vec4 col = applySdf(
		texture(fontTex, vec3(inTex.zw, fontIndexChannelMult[3])),
		vec3(inTex.xy, fontIndexChannelMult[3]),
		stretch
		);
	if (col.a < 0.5)
		discard;

	outPosition = vec4(inWorldPos.xyz, 1.0);
	outNormal = vec4(-viewProj[2].xyz, 1.0);
	outSurface = vec4(1.0);
}

void vs_transformPosTexStretch(
	vec4 inPosition : POSITION,
	vec4 inTex : TEXCOORD,
	vec4 inColor : COLOR0,
	int instanceID : gl_InstanceID,
	out vec4 outPosition : gl_Position,
	out vec4 outTexCoord : TEXCOORD0,
	out vec4 outColor : COLOR0,
	out float stretch : TEXCOORD1,
	out vec4 channelMultAndFontIndex : FONT_INDEX_CHANNEL_MULT)
{
	int index = int(inPosition.w) + instanceID;
	ivec2 texOffset = ivec2((index % numMetaPerRow) * 6, index  / numMetaPerRow);
	mat4 world = mat4(
		texelFetch(metaSampler, texOffset, 0),
		texelFetch(metaSampler, ivec2(texOffset.x + 1, texOffset.y), 0),
		texelFetch(metaSampler, ivec2(texOffset.x + 2, texOffset.y), 0),
		texelFetch(metaSampler, ivec2(texOffset.x + 3, texOffset.y), 0)
		) * transform;

	outColor = inColor * texelFetch(metaSampler, ivec2(texOffset.x + 4, texOffset.y), 0);
	channelMultAndFontIndex = texelFetch(metaSampler, ivec2(texOffset.x + 5, texOffset.y), 0);

	mat4 worldViewProj = world * viewProj;
	outPosition = vec4(inPosition.xy, 0.0, 1.0) * worldViewProj;
	vec4 s = vec4(inPosition.x + inPosition.z, inPosition.y + inPosition.z, 0.0, 1.0) * worldViewProj;
	stretch = length(outPosition.xy / outPosition.z - s.xy / s.z) / 1.41;
	vec4 bounds = textureBounds[int(channelMultAndFontIndex [3])];
	outTexCoord = vec4(inTex * (bounds / maxBounds));
}

void vs_transformPosWorldTex(
	vec4 inPosition : POSITION,
	vec4 inTex : TEXCOORD,
	int instanceID : gl_InstanceID,
	out vec4 outPosition : gl_Position,
	out vec3 outWorldPos : TEXCOORD2,
	out vec4 outTex : TEXCOORD0,
	out float stretch : TEXCOORD1,
	out vec4 channelMultAndFontIndex : FONT_INDEX_CHANNEL_MULT
) {
	int index = int(inPosition.w) + instanceID;
	ivec2 texOffset = ivec2((index % numMetaPerRow) * 6, index  / numMetaPerRow);
	mat4 world= mat4(
		texelFetch(metaSampler, texOffset, 0),
		texelFetch(metaSampler, ivec2(texOffset.x + 1, texOffset.y), 0),
		texelFetch(metaSampler, ivec2(texOffset.x + 2, texOffset.y), 0),
		texelFetch(metaSampler, ivec2(texOffset.x + 3, texOffset.y), 0)
		) * transform;

	channelMultAndFontIndex = texelFetch(metaSampler, ivec2(texOffset.x + 5, texOffset.y), 0);

	outWorldPos = (vec4(inPosition.xyz, 1.0) * world).xyz;
	mat4 worldViewProj = world * viewProj;
	outPosition = vec4(inPosition.xy, 0.0, 1.0) * worldViewProj;
	vec4 s = vec4(inPosition.x + inPosition.z, inPosition.y + inPosition.z, 0.0, 1.0) * worldViewProj;
	stretch = length(outPosition.xy / outPosition.z - s.xy / s.z) / 1.41;
	vec4 bounds = textureBounds[int(channelMultAndFontIndex [3])];
	outTex = vec4(inTex * (bounds / maxBounds) );
}

Pass HUD {
	Enable(Blend, true);
	BlendFuncSeperate(SrcAlpha, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	Enable(CullFace, false);
	Enable(DepthTest, false);
	VertexShader = vs_transformPosTexStretch;
	FragmentShader = fs_Text_HUDColored;
}

Pass Material {
	Enable(Blend, true);
	BlendFuncSeperate(SrcAlpha, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	Enable(CullFace, false);
	Enable(DepthTest, true);
	DepthFunc(Lequal);
	DepthMask(false);
	VertexShader = vs_transformPosTexStretch;
	FragmentShader = fs_Text_MaterialColored;
}

Pass World {
	Enable(CullFace, false);
	Enable(DepthTest, true);
	DepthMask(true);
	VertexShader = vs_transformPosWorldTex;
	FragmentShader = fs_Text_World;
}
