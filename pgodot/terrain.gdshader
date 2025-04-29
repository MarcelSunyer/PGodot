shader_type spatial;

uniform float height = 64.0;
uniform sampler2D colorGradient;
uniform sampler2D normalMap;

void fragment() {
	vec4 worldVertex = INV_VIEW_MATRIX * vec4(VERTEX, 1.0);
	vec3 modelVertex = (inverse(MODEL_MATRIX) * worldVertex).xyz;
	float gradient_uv = (modelVertex.y / height) + 0.5;
	
	ALBEDO = texture(colorGradient, vec2(gradient_uv)).rgb;
	NORMAL_MAP = texture(normalMap, UV).xyz;
}