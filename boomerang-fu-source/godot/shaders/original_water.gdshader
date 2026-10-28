shader_type spatial;
render_mode unshaded, fog_disabled, cull_back, depth_draw_always;
// FX/Water_DepthVignette D3D11 forward programs 19/20. Source gamma-space
// depth/vignette + rough dielectric BRDF. Ambient/probe inputs are explicit.
uniform sampler2D source_depth : hint_depth_texture, filter_nearest, repeat_disable;
uniform vec3 source_shallow = vec3(0.399,0.7400168,1.0);
uniform vec3 source_deep = vec3(0.359856,0.62745935,0.918);
uniform vec3 source_vignette = vec3(0.206423,0.4131249,0.797);
uniform float source_depth_offset = 0.0;
uniform float source_shallow_depth = 0.13;
uniform float source_vignette_size = 0.79;
uniform float source_vignette_intensity = 0.92;
uniform vec4 source_uv_st = vec4(1.0,1.0,0.0,0.0);
uniform vec3 source_sun_direction = vec3(0.0,0.0,1.0);
uniform vec3 source_sun_color = vec3(1.0);
uniform vec3 source_ambient_gamma = vec3(0.0);
uniform vec3 source_reflection_gamma = vec3(0.0);
uniform bool source_use_sh = false;
uniform vec3 source_sh[9];
varying vec3 source_l2;
varying vec3 source_world_normal;
varying vec3 source_world_position;
varying float source_clip_w;
float pow5(float value) { float squared=value*value;return squared*squared*value; }
vec3 gamma_to_linear(vec3 value) {
 return mix(pow((value+vec3(0.055))/1.055,vec3(2.4)),value/12.92,lessThanEqual(value,vec3(0.04045)));
}
void vertex() {
 source_world_normal=normalize(MODEL_NORMAL_MATRIX*NORMAL);
 source_world_position=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;
 vec3 n=vec3(source_world_normal.xy,-source_world_normal.z);
 source_l2=source_sh[4]*n.x*n.y+source_sh[5]*n.y*n.z+source_sh[6]*(3.0*n.z*n.z-1.0)+source_sh[7]*n.x*n.z+source_sh[8]*(n.x*n.x-n.y*n.y);
 source_clip_w=(PROJECTION_MATRIX*MODELVIEW_MATRIX*vec4(VERTEX,1.0)).w;
 vec2 native_uv=vec2(UV.x,1.0-UV.y)*source_uv_st.xy+source_uv_st.zw;
 UV=native_uv;
}
void fragment() {
 if(IN_SHADOW_PASS) { discard; } // Original shader has no ShadowCaster pass.
 float raw_depth=texture(source_depth,SCREEN_UV).r;
 #if CURRENT_RENDERER == RENDERER_COMPATIBILITY
 vec3 ndc=vec3(SCREEN_UV,raw_depth)*2.0-1.0;
 #else
 vec3 ndc=vec3(SCREEN_UV*2.0-1.0,raw_depth);
 #endif
 vec4 behind=INV_PROJECTION_MATRIX*vec4(ndc,1.0);
 float eye_depth=-behind.z/behind.w;
 float blend=clamp((abs(eye_depth-source_clip_w)-source_depth_offset)*source_shallow_depth,0.0,1.0);
 blend=blend*blend*(3.0-2.0*blend);
 vec2 vignette_uv=(UV*2.0-1.0)*source_vignette_size;
 float vignette=min(pow(dot(vignette_uv,vignette_uv),source_vignette_intensity),1.0);
 vec3 base=mix(source_shallow,source_deep,blend)*mix(vec3(1.0),source_vignette,vignette);
 vec3 normal=normalize(source_world_normal);
 vec3 view=normalize(INV_VIEW_MATRIX[3].xyz-source_world_position);
 vec3 sun=normalize(source_sun_direction);
 vec3 half_sum=view+sun;
 vec3 half_direction=half_sum*inversesqrt(max(dot(half_sum,half_sum),0.001));
 float nv=abs(dot(normal,view));
 float nl=clamp(dot(normal,sun),0.0,1.0);
 float lh=clamp(dot(sun,half_direction),0.0,1.0);
 float fd90=2.0*lh*lh-0.5;
 float diffuse=(1.0+fd90*pow5(1.0-nl))*(1.0+fd90*pow5(1.0-nv));
 float specular=sqrt(max(0.5/(nv+nl+0.00001),0.0001))*nl*(0.220916+0.779084*pow5(1.0-lh));
 vec3 ambient=source_ambient_gamma;
 if(source_use_sh) {
  vec3 n=vec3(source_world_normal.xy,-source_world_normal.z);
  vec3 linear_gi=source_sh[0]+source_sh[1]*n.y+source_sh[2]*n.z+source_sh[3]*n.x+source_l2;
  ambient=max(1.055*pow(max(linear_gi,vec3(0.0)),vec3(0.4166666667))-0.055,vec3(0.0));
 }
 vec3 gamma_result=base*0.779084*(source_sun_color*diffuse*nl+ambient)+source_sun_color*specular+source_reflection_gamma*0.72*0.220916;
 ALBEDO=OUTPUT_IS_SRGB ? gamma_result : gamma_to_linear(gamma_result);
 ALPHA=1.0;
}
