shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;
// Native Mobile/Particles shaders 1104/1105 multiply texture by vertex color.
uniform sampler2D source_main : hint_default_white, filter_linear_mipmap, repeat_enable;
vec3 gamma_to_linear(vec3 v) {return mix(pow((v+vec3(0.055))/1.055,vec3(2.4)),v/12.92,lessThanEqual(v,vec3(0.04045)));}
void fragment(){vec4 c=texture(source_main,UV)*COLOR;ALBEDO=gamma_to_linear(c.rgb);ALPHA=c.a;}
