import vs_fullscreen from ChaosGraphics.VertexShaders;

sampler2D renderResultSampler;
sampler2D normalSampler;
sampler2D positionSampler;
vec4 pixelOffset;
vec4 remapRect;

void fs_sample(
	vec2 texCoord : TEXCOORD0,
	out vec4 result : COLOR0
) {
	vec2 inTex = texCoord;
	vec3 nor_x0y0 = normalize(texture(normalSampler, inTex).rgb);
	vec3 nor_xny0 = normalize(texture(normalSampler, inTex + vec2(-pixelOffset.x, 0.0)).rgb);
	vec3 nor_xpy0 = normalize(texture(normalSampler, inTex + vec2(pixelOffset.x, 0.0)).rgb);
	vec3 nor_x0yn = normalize(texture(normalSampler, inTex + vec2(0.0, -pixelOffset.y)).rgb);
	vec3 nor_x0yp = normalize(texture(normalSampler, inTex + vec2(0.0, pixelOffset.y)).rgb);

	vec4 positionSample = texture(positionSampler, inTex);
	if(positionSample.a <= 0.0)
		discard;

	vec3 pos_x0y0 = positionSample.rgb;
	vec3 pos_xny0 = texture(positionSampler, inTex + vec2(-pixelOffset.x, 0.0)).rgb;
	vec3 pos_xpy0 = texture(positionSampler, inTex + vec2(pixelOffset.x, 0.0)).rgb;
	vec3 pos_x0yn = texture(positionSampler, inTex + vec2(0.0, -pixelOffset.y)).rgb;
	vec3 pos_x0yp = texture(positionSampler, inTex + vec2(0.0, pixelOffset.y)).rgb;

	float weight_xny0 = max(0, dot(nor_x0y0, nor_xny0)); weight_xny0 = 1.0 - weight_xny0 * weight_xny0 + min(1.0, length(pos_x0y0 - pos_xny0)) * pixelOffset.z;
	float weight_xpy0 = max(0, dot(nor_x0y0, nor_xpy0)); weight_xpy0 = 1.0 - weight_xpy0 * weight_xpy0 + min(1.0, length(pos_x0y0 - pos_xpy0)) * pixelOffset.z;
	float weight_x0yn = max(0, dot(nor_x0y0, nor_x0yn)); weight_x0yn = 1.0 - weight_x0yn * weight_x0yn + min(1.0, length(pos_x0y0 - pos_x0yn)) * pixelOffset.z;
	float weight_x0yp = max(0, dot(nor_x0y0, nor_x0yp)); weight_x0yp = 1.0 - weight_x0yp * weight_x0yp + min(1.0, length(pos_x0y0 - pos_x0yp)) * pixelOffset.z;
	float weightSumOther = weight_xny0 + weight_xpy0 + weight_x0yn + weight_x0yp;
	float finalAlpha = pixelOffset.w * weightSumOther / 4.0;

	vec3 col_x0y0 = texture(renderResultSampler, inTex).rgb;
	vec3 col_xny0 = texture(renderResultSampler, inTex + vec2(-pixelOffset.x, 0.0)).rgb;
	vec3 col_xpy0 = texture(renderResultSampler, inTex + vec2(pixelOffset.x, 0.0)).rgb;
	vec3 col_x0yn = texture(renderResultSampler, inTex + vec2(0.0, -pixelOffset.y)).rgb;
	vec3 col_x0yp = texture(renderResultSampler, inTex + vec2(0.0, pixelOffset.y)).rgb;

	vec3 color = ((col_x0y0 * 2.0 + col_xny0 * weight_xny0
				   + col_xpy0 * weight_xpy0
				   + col_x0yn * weight_x0yn
				   + col_x0yp * weight_x0yp) / (2.0 + weightSumOther)).rgb;
	result = vec4(col_x0y0 + (color - col_x0y0) * clamp(finalAlpha, 0.0, 1.0), 1.0);
}

void vs_remap(
	out vec4 outPosition : gl_Position,
	out vec2 fsTexCoord : TEXCOORD0
) {
	float y = float(gl_VertexID % 2);
	float x = float(gl_VertexID / 2);
	outPosition = vec4(-1.0 + x*4.0, -1.0+y*4.0, 0.0, 1.0);
	fsTexCoord = vec2(x * 2.0, y * 2.0) * remapRect.zw + remapRect.xy;
}

Pass FXAA {
	Enable(Blend, false);
	Enable(DepthTest, false);
	Enable(CullFace, false);
	VertexShader = vs_fullscreen;
	FragmentShader = fs_sample;
}

Pass FXAA_Remap {
	Enable(Blend, false);
	Enable(DepthTest, false);
	Enable(CullFace, false);
	VertexShader = vs_remap;
	FragmentShader = fs_sample;
}
