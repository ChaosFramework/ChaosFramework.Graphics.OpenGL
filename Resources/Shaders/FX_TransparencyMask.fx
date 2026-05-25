import ChaosGraphics.ParticleSystem;
import vs_fullscreen from ChaosGraphics.VertexShaders;
import vs_createSprite, vs_createSpriteInstanced from ChaosGraphics.Sprite;
import tex from ChaosGraphics.Sprite;

#define maskTexBias 0.1

vec4 screenSize : SCREEN_SIZE;

int sampleLayer;
sampler2D compareSampler;
sampler2D solidSampler;

float zNear, zFar;
float solidDepthBias;

float linearizeDepth(float z_b) {
    float z_n = 2.0 * z_b - 1.0;
    return 2.0 * zNear * zFar / (zFar + zNear - z_n * (zFar - zNear));
}

void fs_copyScreen(vec2 inTex : TEXCOORD0, out vec4 outColor : COLOR0) {
	outColor = texelFetch(solidSampler, ivec2(gl_FragCoord.xy), 0);
}

//Renders the mask for transparents with a solid world to consider
void fs_createTransparencyMask() {
	vec2 inTex = gl_FragCoord.xy / screenSize.zw;
	float comparePixel = texelFetch(compareSampler, ivec2(gl_FragCoord.xy), 0).r;
	float solidDepth = texture(solidSampler, inTex).r;
	//Clip behind the real world.
	//Clip if the current pixel has the same depth or less than has already been rendered
	bool solidWorldOcclusion = linearizeDepth(gl_FragCoord.z) > linearizeDepth(solidDepth) + solidDepthBias;
	if (solidWorldOcclusion || (gl_FragCoord.z <= comparePixel && comparePixel != 0.0))
		discard;
}

//Renders the mask for transparents with a solid world to consider
void fs_createTransparencyMaskNoWorld() {
	float comparePixel = texelFetch(compareSampler, ivec2(gl_FragCoord.xy), 0).r;
	//Clip if the current pixel has the same depth or less than has already been rendered
	if ((gl_FragCoord.z <= comparePixel && comparePixel != 0.0))
		discard;
}

void fs_createMaskTextured(vec2 maskTexCoord : TEXCOORD0) {
	if (texture(tex, maskTexCoord).a < maskTexBias)
		discard;
	fs_createTransparencyMask();
}

void fs_createMaskTexturedNoWorld(vec2 maskTexCoord : TEXCOORD0) {
	if (texture(tex, maskTexCoord).a < maskTexBias)
		discard;
	fs_createTransparencyMaskNoWorld();
}

void vs_setDepth() {
	gl_Position.z = 1.0;
}

void vs_transformPositionInstanced(vec3 inPosition : POSITION,
	mat4 instanceTransform : INSTANCE_TRANSFORM,
	out vec4 outPosition : gl_Position
) {
	outPosition = ((vec4(inPosition, 1.0) * instanceTransform) * viewProj);
}

Pass CopyScreen
{
	Enable(DepthTest, false);
	Enable(CullFace, false);
	Enable(Blend, false);
	VertexShader = vs_fullscreen, vs_setDepth;
	FragmentShader = fs_copyScreen;
}

Pass Mesh
{
	Enable(CullFace, false);
	VertexShader = vs_transformPosition;
	FragmentShader = fs_createTransparencyMask;
}

Pass MeshInstanced
{
	Enable(CullFace, false);
	VertexShader = vs_transformPositionInstanced;
	FragmentShader = fs_createTransparencyMask;
}

Pass Sprite
{
	Enable(CullFace, false);
	VertexShader = vs_createSprite;
	FragmentShader = fs_createTransparencyMask;
}

Pass SpriteInstanced
{
	Enable(CullFace, false);
	VertexShader = vs_createSpriteInstanced;
	FragmentShader = fs_createMaskTextured;
}

Pass ParticleInstanced
{
	Enable(CullFace, false);
	VertexShader = vs_createParticleInstanced;
	FragmentShader = fs_createMaskTextured;
}

Pass SpriteInstancedNoWorld
{
	Enable(CullFace, false);
	VertexShader = vs_createSpriteInstanced;
	FragmentShader = fs_createMaskTexturedNoWorld;
}

Pass ParticleInstancedNoWorld
{
	Enable(CullFace, false);
	VertexShader = vs_createParticleInstanced;
	FragmentShader = fs_createMaskTexturedNoWorld;
}
