shader_type spatial;
render_mode wireframe;

const float color_mult = 1.0f;
uniform float brightness: hint_range(0.0f, 1.0f) = 1.0f;
uniform sampler2D grass_texture;
uniform sampler2D dirt_texture;

void vertex() {

}

void fragment() {
	vec4 projected_coords = INV_VIEW_MATRIX * vec4(VERTEX, 1.0);
	vec3 albedoX = texture(dirt_texture, projected_coords.zy).rgb;
	vec3 albedoY = texture(grass_texture, projected_coords.xz).rgb;
	vec3 albedoZ = texture(dirt_texture, projected_coords.xy).rgb;
	vec3 world_normal = abs(INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz;
	vec3 normal_weights = world_normal / (world_normal.x + world_normal.y + world_normal.z);
	
	vec3 color_offset = vec3(0,0,1);
	///random(floor(projected_coords.xyz));
	
	ALBEDO = mix(vec3(0.0f), mix((albedoX * normal_weights.x + albedoY * normal_weights.y + albedoZ * normal_weights.z), color_offset.rgb, 0.05f), brightness);


}