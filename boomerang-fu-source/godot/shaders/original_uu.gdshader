shader_type spatial;
render_mode ambient_light_disabled, specular_disabled, fog_disabled, cull_back;
// Translated from the source D3D11 programs; see shader-native-disassembly.json.
// Source uses Gamma rendering and ignores light color/intensity in these passes.
uniform vec4 source_albedo = vec4(1.0);
uniform vec4 source_add = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec3 source_shadow = vec3(1.0);
uniform vec3 source_tint = vec3(1.0);
uniform vec3 source_sun_direction = vec3(0.0, 0.0, 1.0);
uniform sampler2D source_main : hint_default_white, filter_linear_mipmap, repeat_enable;
uniform sampler2D source_grain : hint_default_white, filter_linear_mipmap, repeat_enable;
uniform sampler2D source_ramp : hint_default_white, filter_linear, repeat_disable;
uniform sampler2D source_noise : hint_default_white, filter_linear_mipmap, repeat_enable;
uniform sampler2D source_tile : hint_default_white, filter_linear_mipmap, repeat_enable;
uniform vec4 source_main_st = vec4(1.0, 1.0, 0.0, 0.0);
uniform vec4 source_grain_st = vec4(1.0, 1.0, 0.0, 0.0);
uniform vec4 source_tile_st = vec4(1.0, 1.0, 0.0, 0.0);
uniform vec4 source_flash = vec4(0.0);
uniform bool source_has_topmapped = false;
uniform bool source_has_grain = false;
uniform bool source_has_ramp = false;
uniform bool source_cutout = false;
uniform bool source_plain_cutout = false;
uniform float source_cutoff = 0.5;
uniform bool source_height_fog = false;
uniform vec2 source_fog_depth = vec2(0.0, 1.0);
uniform vec3 source_fog_color = vec3(1.0);
varying float original_ndotl;
varying vec3 original_light_view;
varying vec2 original_grain_uv;
varying vec2 original_tile_uv;
varying float original_height;
varying vec4 original_screen;
varying vec3 original_base;
varying vec3 original_fog_target;
varying float original_fog_amount;

vec3 source_gamma_to_linear(vec3 value) {
    return mix(pow((value + vec3(0.055)) / 1.055, vec3(2.4)), value / 12.92, lessThanEqual(value, vec3(0.04045)));
}

void vertex() {
    vec3 world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
    original_ndotl = max(dot(world_normal, source_sun_direction), 0.0);
    original_light_view = mat3(VIEW_MATRIX) * source_sun_direction;
    vec3 world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    original_grain_uv = world_position.yx * source_grain_st.xy + source_grain_st.zw;
    original_grain_uv.y = 1.0 - original_grain_uv.y;
    original_tile_uv = world_position.yx * source_tile_st.xy + source_tile_st.zw;
    original_tile_uv.y = 1.0 - original_tile_uv.y;
    original_height = -world_position.z;
    original_screen = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
    original_screen.xy = (original_screen.xy + original_screen.ww) * 0.5;
    // Imported mesh UVs already reflect the texture's vertical origin.
    vec2 source_uv = vec2(UV.x, 1.0 - UV.y) * source_main_st.xy + source_main_st.zw;
    UV = vec2(source_uv.x, 1.0 - source_uv.y);
}

void fragment() {
    vec4 sample_color = texture(source_main, UV);
    vec4 base = sample_color * source_albedo;
    if (source_has_topmapped) {
        // Original floor shader: mesh alpha masks its edge texture over the
        // world-space stone tile; warning flash is added after shadowing.
        base = vec4(mix(texture(source_tile, original_tile_uv).rgb, sample_color.rgb, sample_color.a), 1.0) * source_albedo;
    }
    if (source_plain_cutout) { base = sample_color; }
    if (source_cutout && base.a < source_cutoff) { discard; }
    if (source_has_grain) { base *= texture(source_grain, original_grain_uv); }
    if (!source_cutout && !source_height_fog && !source_has_topmapped) { base += source_add; }
    original_base = base.rgb * COLOR.rgb * source_tint;
    ALBEDO = vec3(1.0);
    if (source_plain_cutout) { ALBEDO = source_gamma_to_linear(sample_color.rgb); }
    if (source_height_fog) {
        vec2 noise_uv = vec2(original_screen.x, 1.0 - original_screen.y);
        float noisy_height = original_height + texture(source_noise, noise_uv).r * 0.25;
        original_fog_amount = clamp((noisy_height - source_fog_depth.x) / (source_fog_depth.y - source_fog_depth.x), 0.0, 1.0);
        original_fog_target = pow(source_fog_color, vec3(1.0 - original_screen.y / original_screen.w));
    }
    SPECULAR = 0.0;
}

void light() {
    // Unity's other two lights are ForceVertex and not sampled by these shaders.
    if (dot(LIGHT, original_light_view) > 0.999) {
        float shade = min(original_ndotl, 1.0) * ATTENUATION;
        if (source_has_ramp) { shade = texture(source_ramp, vec2(shade, 0.5)).r; }
        vec3 result = original_base * clamp(source_shadow + vec3(shade), vec3(0.0), vec3(1.0));
        if (source_has_topmapped) { result += source_flash.rgb * source_flash.a; }
        if (source_height_fog) { result = mix(result, original_fog_target, original_fog_amount); }
        // Godot's final framebuffer transfer encodes linear color as sRGB.
        // Preserve Unity's Gamma-space formula until that final transfer.
        DIFFUSE_LIGHT += source_gamma_to_linear(result);
    }
}
