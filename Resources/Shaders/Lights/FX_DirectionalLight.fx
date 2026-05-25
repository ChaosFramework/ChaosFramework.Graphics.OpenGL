import ChaosGraphics.Shade;

void fs_DirectionalLight(
	vec3 direction : DIRECTION,
	vec4 color : LIGHTCOLOR,
	vec4 ambient : AMBIENTCOLOR,
	out vec4 result : COLOR0
) {
	ivec2 texelCoords = ivec2(gl_FragCoord.xy);

	vec3 normal;
	vec4 diffuse, specular;
	sampleLightData(texelCoords, normal, diffuse, specular);
	vec3 worldPos = texelFetch(positionSampler, texelCoords, 0).xyz;

	result = shade(worldPos, normal, diffuse, specular, direction, color, ambient);
}

vec4 shade(
	vec3 worldPos,
	vec3 normal,
	vec4 diffuse,
	vec4 specular,
	vec3 direction,
	vec4 color,
	vec4 ambient
) {
	return shadeDefault(diffuse, specular, worldPos, normal, direction.xyz, 1.0) * color + diffuse * ambient;
}

void vs_fullscreen(
	out vec4 outPosition : gl_Position
) {
	float y = float(gl_VertexID % 2);
	float x = float(gl_VertexID / 2);
	outPosition = vec4(-1.0 + x*4.0, -1.0 + y*4.0, 0.0, 1.0);
}

Pass Simple{
	Enable(Blend, true);
	Enable(DepthTest, false);
	Enable(CullFace, false);
	VertexShader = vs_fullscreen, PASS(vec3 DIRECTION), PASS(vec4 LIGHTCOLOR), PASS(vec4 AMBIENTCOLOR);
	FragmentShader = fs_DirectionalLight;
}