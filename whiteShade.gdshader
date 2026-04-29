shader_type spatial;
render_mode diffuse_toon, specular_toon; // Use toon lighting

uniform vec4 base_color : source_color = vec4(1.0); // White
uniform float cel_threshold = 1.5;
uniform float glow_strength : hint_range(0.0, 7.0) = 3.0;

void fragment() {
    ALBEDO = base_color.rgb;
    vec3 white = vec3(1.0, 1.0, 1.0);
	EMISSION = white * glow_strength;
}

void light() {
    // Cel shading logic: clamp diffuse lighting to a sharp edge
    float NdotL = dot(NORMAL, LIGHT);
    float cel = smoothstep(cel_threshold - 0.01, cel_threshold + 0.01, NdotL);
    // Apply cel shading color (mix of base and shadow color)
    DIFFUSE_LIGHT += LIGHT_COLOR * cel;
}