#define numSamples 13

import vs_fullscreen from ChaosGraphics.VertexShaders;

sampler2D srcSampler;

float blurRange;
float sampleWeight[13];

void computePixelHorizontal(
	vec2 tex : TEXCOORD0,
	out vec4 outCol : COLOR0
) {
	outCol = vec4(0.0);
	for (int i = 0; i < numSamples; i++)
		outCol += texture(srcSampler, vec2(tex.x + blurRange * (i - (numSamples - 1) * 0.5), tex.y)) * sampleWeight[i];
}

void computePixelVertical(
	vec2 tex : TEXCOORD0,
	out vec4 outCol : COLOR0
) {
	outCol = vec4(0.0);
	for (int i = 0; i < numSamples; i++)
		outCol += texture(srcSampler, vec2(tex.x, tex.y + blurRange * (i - (numSamples - 1) * 0.5))) * sampleWeight[i];
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