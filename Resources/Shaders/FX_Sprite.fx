import ChaosGraphics.VertexShaders;

sampler2D tex;

void fs_sample(vec2 inTex : TEXCOORD0, out vec4 outColor : COLOR0)
{
	outColor = texture(tex, inTex);
	outColor.a = clamp(outColor.a, 0.0, 1.0);
}

void fs_sampleClamp(vec2 inTex : TEXCOORD0, out vec4 outColor : COLOR0)
{
	fs_sample(inTex, outColor);
	outColor.a = clamp(outColor.a, 0.0, 1.0);
}

Pass Sprite
{
	Enable(Blend, true);
	BlendFuncSeperate(SrcAlpha, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	VertexShader = vs_createSprite;
	FragmentShader = fs_sample;
}

Pass Instanced
{
	Enable(Blend, true);
	BlendFuncSeperate(SrcAlpha, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	VertexShader = vs_createSpriteInstanced;
	FragmentShader = fs_sample;
}

Pass Screen
{
	Enable(Blend, true);
	Enable(DepthTest, false);
	BlendFuncSeperate(SrcAlpha, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	Enable(CullFace, false);
	VertexShader = vs_fullscreen;
	FragmentShader = fs_sample;
}

Pass ScreenCustomBlend
{
	Enable(CullFace, false);
	Enable(DepthTest, false);
	VertexShader = vs_fullscreen;
	FragmentShader = fs_sample;
}

Pass SpriteClamp
{
	Enable(Blend, true);
	BlendFuncSeperate(SrcAlpha, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	VertexShader = vs_createSprite, vs_transformPosition;
	FragmentShader = fs_sampleClamp;
}

Pass ScreenClamp
{
	Enable(Blend, true);
	BlendFuncSeperate(SrcAlpha, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	Enable(CullFace, false);
	Enable(DepthTest, false);
	VertexShader = vs_fullscreen;
	FragmentShader = fs_sampleClamp;
}
