shader_type spatial;

uniform float height = 64.0;
uniform sampler2D gradient_tex;

void fragment() {
    // Convertimos la posición del vértice de mundo a espacio local
    vec4 worldVertex = INV_VIEW_MATRIX * vec4(VERTEX, 1.0);
    vec3 modelVertex = (inverse(MODEL_MATRIX) * worldVertex).xyz;

    // Normalizamos la altura en función del rango máximo
    float normalized_height = clamp(modelVertex.y / height, 0.0, 1.0);

    // Muestreamos el gradiente (en vertical usamos 0.5 ya que es una textura 1D "emulada")
    vec2 gradient_uv = vec2(normalized_height, 0.5);
    ALBEDO = texture(gradient_tex, gradient_uv).rgb;
}
