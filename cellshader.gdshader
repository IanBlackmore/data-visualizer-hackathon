shader_type spatial;
render_mode cull_front, unshaded; // Render backfaces only, no lighting

uniform float outline_width = 0.01;
uniform vec4 outline_color : source_color = vec4(0.0, 0.0, 0.0, 1.0); // Black

void vertex() {
    // Grow the mesh along its normals
    VERTEX += NORMAL * outline_width;
}

void fragment() {
    ALBEDO = outline_color.rgb;
}
