shader_type spatial;

uniform float height = 64.0;
uniform sampler2D gradient_tex;
uniform sampler2D normalMap;

void fragment() {
    vec4 worldVertex = INV_VIEW_MATRIX * vec4(VERTEX, 1.0);
    vec3 modelVertex = (inverse(MODEL_MATRIX) * worldVertex).xyz;
    
    // Normalizar la altura entre 0 y 1 usando la altura máxima
    float normalized_height = clamp(modelVertex.y / height, 0.0, 1.0);
    
    // Usar la altura normalizada como coordenada V (vertical) en la textura
    // Usamos 0.5 para la coordenada U (horizontal) para tomar el centro del gradiente
    ALBEDO = texture(gradient_tex, vec2(0.5, normalized_height)).rgb;
    NORMAL_MAP = texture(normalMap, UV).xyz;
}