shader_type spatial;
render_mode unshaded, blend_mix, cull_back, depth_draw_never, fog_disabled;
// Native D3D11: UU/Unlit/Alpha/Blend (1115), Legacy/Particles/Additive (1099).
uniform sampler2D source_main : hint_default_white, filter_linear_mipmap, repeat_enable;
uniform vec4 source_tint_color=vec4(1.0);
uniform vec4 source_st=vec4(1.0,1.0,0.0,0.0);
uniform float source_multiplier=1.0;
vec3 gamma_to_linear(vec3 v){return mix(pow((v+vec3(0.055))/1.055,vec3(2.4)),v/12.92,lessThanEqual(v,vec3(0.04045)));}
void vertex(){vec2 uv=vec2(UV.x,1.0-UV.y)*source_st.xy+source_st.zw;UV=vec2(uv.x,1.0-uv.y);}
void fragment(){vec4 c=texture(source_main,UV)*COLOR*source_tint_color*source_multiplier;ALBEDO=gamma_to_linear(c.rgb);ALPHA=clamp(c.a,0.0,1.0);}
