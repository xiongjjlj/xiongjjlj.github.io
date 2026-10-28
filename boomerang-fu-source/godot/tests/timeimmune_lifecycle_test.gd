extends SceneTree
const C = preload("res://core/original_timekeeper.gd")
var differences: Array = []
var mismatch_count := 0
func compare(actual: Variant, expected: Variant, path: String) -> void:
	if actual is Dictionary:
		for key in actual: compare(actual[key],expected[key],path+"/"+str(key))
	elif actual is Array:
		for i in actual.size(): compare(actual[i],expected[i],path+"/"+str(i))
	else:
		var reference: Variant = C.f32(expected) if actual is float else expected
		if actual != reference:
			mismatch_count += 1
			if differences.size()<30: differences.append({"path":path,"actual":actual,"expected":reference})
func _initialize() -> void:
	var fixture: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/timeimmune-native-lifecycle.json"))
	var frame_count := 0
	for item in fixture.cases:
		var clock := C.new()
		var weapons: Array[OriginalPlayerWeapons] = []
		var discs: Array[OriginalDiscMotion] = []
		var owners := [-1,-1]
		for i in 2:
			var weapon := OriginalPlayerWeapons.new()
			weapon.initialize(OriginalActorMotion.new(),OriginalCollisionWorld.new())
			weapons.append(weapon)
			discs.append(OriginalDiscMotion.new())
		for frame in item.frames:
			for action in frame.actions:
				var index := int(action.target)
				match action.op:
					"grant":
						weapons[index].powers.start(4096)
						clock.slow_mo_time_warp(6.3,0.2,1.0,0.4)
					"disable": clock.enable_slow_mo = false
					"target": clock.set_time_scale(C.f32(action.value))
					"locked": clock.end_match()
					"freeze": clock.freeze(C.f32(action.value))
					"pause": clock.pause_time()
					"unpause": clock.unpause_time()
					"remove": weapons[index].powers.clear()
					"death": weapons[index].powers.death_time_immune(clock,weapons.map(func(w):return w.powers))
					"death_slow": clock.slow_mo_effect(0.5,0.5)
					"throw":
						owners[index] = int(action.value)
						discs[index].owner_actor = weapons[int(action.value)].actor
						discs[index].time_scaler = discs[index].owner_actor.time_scaler
						discs[index].start_throw()
					"owner":
						owners[index] = int(action.value)
						discs[index].owner_actor = weapons[owners[index]].actor if owners[index]>=0 else null
			var dt := C.f32(frame.dt)
			var delta := C.f32(dt * clock.time_scale)
			for weapon in weapons:
				weapon.powers.update_time_immune(delta,clock.time_scale,clock.target_scale,int(frame.game_state)==C.GameState.PLAYING)
				weapon.powers.events.clear()
			for disc in discs: disc.update_time_scaler(delta,clock.time_scale)
			clock.update(dt,int(frame.game_state))
			var players: Array = []
			var disc_states: Array = []
			for i in 2:
				players.append({"active":weapons[i].powers.active,"scale":weapons[i].actor.time_scaler,"timer":weapons[i].powers.time_immune_timer})
				disc_states.append({"owner":owners[i],"scale":discs[i].time_scaler,"cooldown":discs[i].time_scaler_cooldown,"requests":discs[i].time_scaled_contact_requests})
			var actual := {"players":players,"discs":disc_states,"clock":{"scale":clock.time_scale,"time":clock.time_counter,"unscaled":clock.unscaled_time_counter,"target":clock.target_scale,"slow":clock.slow_mo,"freeze":clock.freeze_frame,"state":clock.slow_mo_state}}
			compare(actual,frame.expected,item.name+"/"+str(frame.frame))
			frame_count += 1
	var report := {"passed":mismatch_count==0,"cases":fixture.cases.size(),"frames":frame_count,"mismatch_count":mismatch_count,"differences":differences,"scope":fixture.scope}
	FileAccess.open("res://../evidence/godot-native-timeimmune-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t"))
	print("BF_TIMEIMMUNE_CHECK "+JSON.stringify(report))
	quit(0 if mismatch_count==0 else 1)
