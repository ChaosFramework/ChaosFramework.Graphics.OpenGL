#define numSamples 13
#define gain 5.0

import vs_fullscreen from ChaosGraphics.VertexShaders;

sampler2D srcSampler;
sampler2D originalSampler;

float blurRange;
float minimumGlowValue;
float sampleWeight[13];

void computePixelHorizontal(
	vec2 tex : TEXCOORD0,
	out vec4 outCol : COLOR0
) {
	outCol = vec4(0.0);
	for (int i = 0; i < numSamples; i++)
		outCol += gain * max(texture(srcSampler, vec2(tex.x + blurRange * (i - (numSamples - 1.0) * 0.5), tex.y)) - minimumGlowValue, vec4(0.0)) * sampleWeight[i];
}

void computePixelVertical(
	vec2 tex : TEXCOORD0,
	out vec4 outCol : COLOR0
) {
	outCol = vec4(0.0);
	for (int i = 0; i < numSamples; i++)
		outCol += texture(srcSampler, vec2(tex.x, tex.y + blurRange * (i - (numSamples - 1) * 0.5))) * sampleWeight[i];
	outCol += min(vec4(1.0), texture(originalSampler, tex));
}

Pass Horizontal {
	Enable(DepthTest, false);
	FragmentShader = computePixelHorizontal;
	VertexShader = vs_fullscreen;
}

Pass Vertical {
	Enable(DepthTest, false);
	FragmentShader = computePixelVertical;
	VertexShader = vs_fullscreen;
}
