import vs_fullscreen from ChaosGraphics.VertexShaders;
import emissiveSampler, diffuseSampler, alphaThreshold from ChaosGraphics.MaterialDefault;

vec4 screenSize : SCREEN_SIZE;
vec4 ambient;

void fs_renderClip(out vec4 result : COLOR0) {
	vec2 inTex = (gl_FragCoord.xy - screenSize.xy) / screenSize.zw;
	result = texture(emissiveSampler, inTex);
	if (result.a < alphaThreshold)
		discard;
	result.a = 1.0;
	result += texture(diffuseSampler, inTex) * ambient;
}

void fs_render(out vec4 result : COLOR0) {
	vec2 inTex = (gl_FragCoord.xy - screenSize.xy) / screenSize.zw;
	result = texture(emissiveSampler, inTex) + texture(diffuseSampler, inTex) * ambient;
}

Pass render
{
	Enable(DepthTest, false);
	FragmentShader = fs_render;
	VertexShader = vs_fullscreen;
}

Pass renderTested
{
	Enable(DepthTest, false);
	FragmentShader = fs_renderClip;
	VertexShader = vs_fullscreen;
}
