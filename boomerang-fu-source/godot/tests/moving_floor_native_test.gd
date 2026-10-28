extends SceneTree
const F = preload("res://core/original_moving_floors.gd")
const C = preload("res://core/original_timekeeper.gd")
func _initialize() -> void: call_deferred("run")
func read(path: String): return JSON.parse_string(FileAccess.get_file_as_string(path))
func run() -> void:
	var inputs: Dictionary = read("res://../evidence/moving-floor-native-input.json")
	var native: Dictionary = read("res://../evidence/moving-floor-native-motion.json")
	var reports: Array = []
	for i in inputs.cases.size():
		var config: Dictionary = inputs.cases[i]
		var base := Node3D.new(); root.add_child(base)
		var node: Node3D = base
		for pose in config.chain:
			var child := Node3D.new(); node.add_child(child); node = child
			var p: Dictionary = pose.position; var q: Dictionary = pose.rotation; var s: Dictionary = pose.scale
			node.position = Vector3(p.x,p.y,-p.z); node.quaternion = Quaternion(-q.x,-q.y,q.z,q.w); node.scale = Vector3(s.x,s.y,s.z)
		var adapter := F.new(); var world := OriginalCollisionWorld.new()
		adapter.initialize({"behaviours":[config.record]}, {int(config.record.game_object):node}, world, OriginalUnityRandom.new(int(config.seed)))
		adapter.reset(); adapter.start_all()
		var f: Dictionary = adapter.floors.values()[0]
		var errors := {"position":0.0,"rotation":0.0,"velocity":0.0,"percent":0.0,"duration":0.0,"target_duration":0.0,"index":0.0}
		var failures: Array = []
		for sample in native.cases[i].samples:
			if sample.tick > 0: adapter.fixed_step(C.f32(inputs.fixed_delta))
			var q := Quaternion(float(sample.q[0]),float(sample.q[1]),float(sample.q[2]),float(sample.q[3]))
			var expected_angle := atan2(2.0*q.w*q.z,1.0-2.0*q.z*q.z)
			var current := {"position":node.position.distance_to(Vector3(sample.p[0],sample.p[1],-sample.p[2])),"rotation":abs(wrapf(node.rotation.z-expected_angle,-PI,PI)),
				"velocity":f.velocity.distance_to(Vector2(sample.v[0],sample.v[1])),"percent":abs(f.move_percent-C.f32(sample.percent)),
				"duration":abs(f.move_duration-C.f32(sample.duration)),"target_duration":abs(f.new_duration-C.f32(sample.target_duration)),"index":abs(f.pos_index-int(sample.index))}
			for key in errors:
				errors[key] = maxf(errors[key],current[key])
				var tolerance := 0.002 if key == "velocity" else (0.00003 if key == "position" else 0.000002)
				if current[key] > tolerance and failures.size() < 8: failures.append({"tick":sample.tick,"field":key,"error":current[key]})
		var result := {"key":config.key,"samples":native.cases[i].samples.size(),"max_errors":errors,"failures":failures,"passed":failures.is_empty()}
		reports.append(result); print("BF_FLOOR_CASE " + JSON.stringify(result))
		base.free()
	var report := {"passed":reports.all(func(r):return r.passed),"runtime":native.runtime,"cases":reports,"scope":native.scope}
	FileAccess.open("res://../evidence/godot-moving-floor-native-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_MOVING_FLOOR_NATIVE_TEST " + JSON.stringify({"passed":report.passed,"cases":reports.size()}))
	quit(0 if report.passed else 1)
