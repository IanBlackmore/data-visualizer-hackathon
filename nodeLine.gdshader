shader_type spatial;

render_mode unshaded;

uniform float glow_strength : hint_range(0.0, 10.0) = 3.0;

void fragment() {
    vec3 red = vec3(1.0, 0.0, 0.0);

    ALBEDO = red;
    EMISSION = red * glow_strength;
}