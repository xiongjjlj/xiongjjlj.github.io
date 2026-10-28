shader_type spatial;
render_mode unshaded, blend_add, cull_back, depth_draw_always, fog_disabled;
// Native PolygonArsenal/PolyRimLightTransparent base pass, source shader 1131.
// Source additive One/One blending and ZWrite; shadow variants remain separate.
uniform vec3 source_inner=vec3(0.0,0.18231593,0.5283019);
uniform vec3 source_rim=vec3(0.2794118,0.6720082,1.0);
// Native $Globals byte offset 96 binds _RimWidth (not the unused _RimPower).
uniform float source_power=2.3;
uniform float source_glow=0.43;
uniform vec3 source_sun_direction=vec3(0.0,0.0,1.0);
uniform vec3 source_sun_color=vec3(1.0);
varying vec3 source_light_view;
vec3 gamma_to_linear(vec3 v){return mix(pow((v+vec3(0.055))/1.055,vec3(2.4)),v/12.92,lessThanEqual(v,vec3(0.04045)));}
void vertex(){source_light_view=mat3(VIEW_MATRIX)*source_sun_direction;}
void fragment(){float rim=pow(1.0-clamp(dot(VIEW,NORMAL),0.0,1.0),source_power);vec3 c=rim*source_rim*source_glow+max(dot(NORMAL,source_light_view),0.0)*source_inner*source_sun_color;ALBEDO=gamma_to_linear(c);ALPHA=1.0;}
