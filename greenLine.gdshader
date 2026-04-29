shader_type spatial;

render_mode unshaded;

uniform float glow_strength : hint_range(0.0, 10.0) = 3.0;

void fragment() {
    vec3 green = vec3(0.0, 1.0, 0.0);

    ALBEDO = green;
    EMISSION = green * glow_strength;
}