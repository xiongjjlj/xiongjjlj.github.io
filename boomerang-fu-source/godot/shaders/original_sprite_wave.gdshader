shader_type spatial;
render_mode blend_premul_alpha, depth_draw_never, cull_disabled, ambient_light_disabled, specular_disabled, fog_disabled;
// PK/SpritesDiffuseWave, D3D11 vertex blob 8 / fragment blob 44.
// Constants below retain the binary32 values recovered with hex disassembly.
uniform sampler2D source_main : hint_default_white, filter_linear_mipmap, repeat_disable;
uniform vec4 source_main_st = vec4(1.0, 1.0, 0.0, 0.0);
uniform vec4 source_albedo = vec4(1.0);
uniform vec3 source_sun_direction = vec3(0.0, 0.0, 1.0);
uniform vec3 source_sun_color = vec3(0.5);
uniform vec3 source_sh[9];
uniform float source_time = 0.0;
uniform float source_shake_time = 1.0;
uniform float source_shake_speed = 0.12;
uniform float source_shake_bending = 0.589;
varying vec3 original_gi;
varying float original_ndotl;
varying vec3 original_light_view;
varying vec3 original_premul;

vec3 source_gamma_to_linear(vec3 value) {
    return mix(pow((value + vec3(0.055)) / 1.055, vec3(2.4)), value / 12.92, lessThanEqual(value, vec3(0.04045)));
}

void vertex() {
    vec2 uv_native = vec2(UV.x, 1.0 - UV.y);
    vec3 world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec4 phase = world_position.y * vec4(0.024, 0.08, 0.08, 0.2);
    phase += world_position.x * vec4(0.048, 0.06, 0.24, 0.096);
    float time_term = (1.0 - source_shake_time * 2.0 - COLOR.b) * (source_time / 20.0);
    time_term *= COLOR.g + source_shake_speed;
    float amplitude = (COLOR.a + source_shake_bending) * uv_native.y;
    phase += time_term * vec4(1.2, 2.0, 1.6, 4.8);
    vec4 x = fract(phase) * 6.408848762512207 - 3.1415927410125732;
    vec4 square = x * x;
    vec4 cube = x * square;
    vec4 wave = cube * -0.16161616146564484 + x;
    vec4 fifth = square * cube;
    vec4 seventh = square * fifth;
    wave += fifth * 0.008333300240337849;
    wave += seventh * -0.00019840999448206276;
    wave *= amplitude;
    wave *= vec4(0.21538744866847992, 0.35897907614707947, 0.2871832549571991, 0.8615497946739197);
    wave *= wave;
    wave *= wave;
    vec3 displacement = vec3(dot(wave, vec4(0.024, 0.04, -0.12, 0.096)), dot(wave, vec4(0.006, 0.02, -0.02, 0.1)), 0.0);
    VERTEX -= mat3(inverse(MODEL_MATRIX)) * displacement;
    vec3 normal_world = normalize(MODEL_NORMAL_MATRIX * NORMAL);
    original_ndotl = max(dot(normal_world, source_sun_direction), 0.0);
    original_light_view = mat3(VIEW_MATRIX) * source_sun_direction;
    vec3 n = vec3(normal_world.xy, -normal_world.z);
    vec3 linear_gi = source_sh[0] + source_sh[1] * n.y + source_sh[2] * n.z + source_sh[3] * n.x;
    linear_gi += source_sh[4] * n.x * n.y + source_sh[5] * n.y * n.z + source_sh[6] * (3.0 * n.z * n.z - 1.0);
    linear_gi += source_sh[7] * n.x * n.z + source_sh[8] * (n.x * n.x - n.y * n.y);
    original_gi = max(1.0549999475479126 * pow(max(linear_gi, vec3(0.0)), vec3(0.4166666567325592)) - 0.054999999701976776, vec3(0.0));
    uv_native = uv_native * source_main_st.xy + source_main_st.zw;
    UV = vec2(uv_native.x, 1.0 - uv_native.y);
}

void fragment() {
    vec4 sample_color = texture(source_main, UV) * COLOR * source_albedo;
    original_premul = sample_color.rgb * sample_color.a;
    ALPHA = sample_color.a;
    ALBEDO = vec3(1.0);
    SPECULAR = 0.0;
    // The source premultiplies in Gamma space, before framebuffer blending.
    // No extra alpha factor: the RGB already contains the source premultiply.
}

void light() {
    if (dot(LIGHT, original_light_view) > 0.999) {
        vec3 lighting = original_gi + source_sun_color * original_ndotl * ATTENUATION;
        DIFFUSE_LIGHT += source_gamma_to_linear(original_premul * lighting);
    }
}
