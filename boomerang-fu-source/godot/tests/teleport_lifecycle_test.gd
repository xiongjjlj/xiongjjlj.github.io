extends SceneTree
const C = preload("res://core/original_timekeeper.gd")
var differences: Array = []
var mismatch_count := 0
func vector(value: Array) -> Vector2: return Vector2(value[0],value[1])
func v3(value: Vector2,z: float=0.0) -> Array: return [value.x,value.y,z]
func compare(actual: Variant,expected: Variant,path: String) -> void:
	if actual is Dictionary:
		for key in actual: compare(actual[key],expected[key],path+"."+key)
		return
	if actual is Array:
		if actual.size()==expected.size():
			for index in actual.size(): compare(actual[index],expected[index],path+"[%d]"%index)
			return
	if actual is float: expected=C.f32(expected)
	if actual!=expected:
		mismatch_count+=1
		if differences.size()<35: differences.append({"field":path,"actual":actual,"expected":expected})
func _initialize() -> void:
	var fixture: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/teleport-native-lifecycle.json"))
	for item in fixture.cases:
		var world := OriginalCollisionWorld.new()
		var ground := OriginalGroundQueries.new()
		ground.world=world
		for definition in fixture.shapes:
			var shape:Dictionary=definition.duplicate()
			shape.id=world.shapes.size()+1
			shape.layer=int(shape.layer)
			shape.actor=false
			shape.edge_radius=0.0
			shape.vertices=PackedVector2Array()
			for point in definition.vertices: shape.vertices.append(vector(point))
			shape.bounds=Rect2(shape.vertices[0],Vector2.ZERO)
			for point in shape.vertices: shape.bounds=shape.bounds.expand(point)
			world.shapes.append(shape)
		var actor := OriginalActorMotion.new()
		actor.world=world
		var env := OriginalActorEnvironment.new()
		env.initialize(actor,ground)
		var weapon := OriginalPlayerWeapons.new()
		weapon.initialize(actor,world)
		var initial:Dictionary=item.initial
		actor.position=vector(initial.position)
		actor.source_z=C.f32(initial.position[2])
		actor.velocity=vector(initial.velocity)
		actor.input_move=Vector2.RIGHT
		actor.can_dash=initial.can_dash
		actor.is_frozen=initial.frozen
		actor.is_stunned=initial.stunned
		actor.is_showing_off=initial.showing
		actor.is_lunging=initial.lunging
		actor.state=int(initial.state)
		actor.is_falling=initial.falling
		actor.fall_timer=C.f32(initial.fall_timer)
		actor.falls_saved=int(initial.saved)
		actor.is_sliced=initial.sliced
		actor.is_dashing=initial.dashing
		actor.dash_timer=C.f32(initial.dash_timer)
		actor.dash_cooldown_timer=C.f32(initial.cooldown)
		actor.retrieve_charge=C.f32(0.7)
		weapon.powers.active=int(initial.power)
		weapon.held_discs=0
		weapon.mini_discs_required=5
		weapon.mini_discs_collected=int(initial.collected)
		weapon.powers.pending_discs=int(initial.pending)
		weapon.attack_cooldown=C.f32(0.5)
		weapon.teleport.last_timestamp=3.0
		weapon.teleport.forced_index=int(item.selected)
		var actors:Array[OriginalActorMotion]=[actor]
		var weapons:Array[OriginalPlayerWeapons]=[weapon]
		world.register_actor(actor)
		for definition in item.victims:
			var victim:=OriginalActorMotion.new()
			victim.world=world
			victim.position=vector(definition.position)
			victim.is_invulnerable=definition.invulnerable
			victim.is_shielded=definition.shield
			var inventory:=OriginalPlayerWeapons.new()
			inventory.initialize(victim,world)
			actors.append(victim)
			weapons.append(inventory)
			world.register_actor(victim)
		var combat:=OriginalCombatWorld.new()
		combat.initialize(actors,weapons,world)
		var all:Array[OriginalDiscMotion]=[]
		for definition in item.discs:
			var disc:=OriginalDiscMotion.new()
			disc.owner_actor=actor
			disc.owner_inventory=weakref(weapon)
			disc.world=world
			disc.position=vector(definition.position)
			disc.source_z=C.f32(definition.position[2])
			disc.disc_powerups=int(definition.power)
			disc.is_mini_disc=definition.mini
			disc.movement=int(definition.movement)
			disc.can_be_picked_up=false
			disc.can_collide_with_owner=false
			weapon.owned_discs.append(disc)
			all.append(disc)
		compare(weapon.teleport.is_safe(),item.safe,item.name+".safe")
		compare(weapon.teleport.dash(C.f32(17.25)),item.accepted,item.name+".accepted")
		var disc_results:Array=[]
		for disc in all: disc_results.append({"active":disc.active,"can_owner":disc.can_collide_with_owner,"can_pick":disc.can_be_picked_up})
		var deaths:Array=[]
		for index in range(1,actors.size()): deaths.append(int(actors[index].state==OriginalActorMotion.State.DEAD))
		compare({"position":v3(actor.position,actor.source_z),"velocity":v3(actor.velocity),"state":actor.state,"falling":actor.is_falling,"saved":actor.falls_saved,"count":weapon.teleport.count,"timestamp":weapon.teleport.last_timestamp,"retrieve":actor.retrieve_charge,"attack_cooldown":weapon.attack_cooldown,"dashing":actor.is_dashing,"dash_timer":actor.dash_timer,"cooldown":actor.dash_cooldown_timer,"can_dash":actor.can_dash,"owned":weapon.owned_discs.size(),"held":weapon.held_discs,"collected":weapon.mini_discs_collected,"ghost":weapon.ghost_discs,"pending":weapon.powers.pending_discs,"respawns":weapon.respawn_requests,"deaths":deaths,"discs":disc_results},item.result,item.name)
	var report := {"passed":mismatch_count==0,"strict_equal":mismatch_count==0,"cases":fixture.cases.size(),"mismatched_fields":mismatch_count,"first_differences":differences,"scope":"Unchanged native Teleport/Dash/hazard/raycast/overlap/Disc.Explode bodies. Pickup, death, respawn scheduling and cosmetic services injected; supplied random selections replayed. Full RNG stream, Animator, shaders and runtime callback parity unverified.","full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-native-teleport-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	quit(0 if mismatch_count==0 else 1)
