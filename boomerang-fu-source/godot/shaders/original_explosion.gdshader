shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Source Legacy Particles/Alpha Blended, D3D11 fragment blob 14.
uniform sampler2D source_main : hint_default_white, filter_linear_mipmap, repeat_enable;
uniform vec4 source_tint = vec4(1.0);
vec3 gamma_to_linear(vec3 value) {
    return mix(pow((value+vec3(0.055))/1.055,vec3(2.4)),value/12.92,lessThanEqual(value,vec3(0.04045)));
}
void fragment() {
    vec4 value = 2.0 * source_tint * texture(source_main, UV);
    ALBEDO = gamma_to_linear(value.rgb);
    ALPHA = clamp(value.a, 0.0, 1.0);
}
