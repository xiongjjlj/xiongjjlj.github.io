extends SceneTree
const C = preload("res://core/original_timekeeper.gd")
func vector(value: Array) -> Vector3: return Vector3(value[0],value[1],value[2])
func _initialize() -> void:
	var fixture: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/camera-native-framing.json"))
	var differences: Array = []
	var frames := 0
	for item in fixture.cases:
		var rig := OriginalCameraRig.new()
		rig.initialize(item.aspect)
		if rig.maximum_zoom != C.f32(item.maximum_zoom): differences.append({"case":item.name,"field":"maximum_zoom","actual":rig.maximum_zoom,"expected":C.f32(item.maximum_zoom)})
		for index in item.trace.size():
			var state: Dictionary = item.trace[index]
			rig.targets.clear()
			for target in state.targets:
				rig.targets.append({"position":vector(target.position),"size":vector(target.size),"influence":C.f32(target.influence)})
			rig.step(state.delta,state.freeze,state.paused)
			var actual := {"focus":[rig.focus.x,rig.focus.y,rig.focus.z],"position":[rig.position.x,rig.position.y,rig.position.z],"zoom":rig.current_zoom,"target_zoom":rig.target_zoom}
			for key in actual:
				var expected: Variant = state[key].map(func(x):return C.f32(x)) if state[key] is Array else C.f32(state[key])
				if actual[key] != expected and differences.size()<12: differences.append({"case":item.name,"frame":index,"field":key,"actual":actual[key],"expected":expected})
			frames += 1
	var report := {"passed":differences.is_empty(),"cases":fixture.cases.size(),"frames":frames,"differences":differences,"original_player_execution":true,"scope":"Source camera framing with supplied targets and zero noise/shake; unrotated LevelEnclosed; no camera matrix comparison","full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-native-camera-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print(JSON.stringify(report))
	quit(0 if differences.is_empty() else 1)
