shader_type spatial;

render_mode unshaded;

uniform float glow_strength : hint_range(0.0, 10.0) = 3.0;

void fragment() {
    vec3 yellow = vec3(1.0, 1.0, 0.0);

    ALBEDO = yellow;
    EMISSION = yellow * glow_strength;
}