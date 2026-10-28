extends SceneTree
var results: Array = []

func check(label: String, passed: bool) -> void:
	results.append({"case": label, "passed": passed})

func _initialize() -> void:
	var world := OriginalCollisionWorld.new()
	var vertices := PackedVector2Array([Vector2(5,-2),Vector2(7,-2),Vector2(7,2),Vector2(5,2)])
	world.shapes.append({"id": 1,"kind": "polygon","vertices": vertices,"edge_radius": 0.0,
		"layer": 256,"bounds": Rect2(5,-2,2,4),"actor": false})
	var hit := world.circle_cast(Vector2.ZERO, 1.0, Vector2.RIGHT, 10.0, 256)
	check("Sweep stops one radius before wall", not hit.is_empty() and absf(hit.distance - 4.0) < 0.00001 and hit.normal == Vector2.LEFT)
	check("Mask excludes unrelated collider", world.circle_cast(Vector2.ZERO, 1.0, Vector2.RIGHT, 10.0, 512).is_empty())
	check("Original queriesStartInColliders=false", world.circle_cast(Vector2(5,0), 1.0, Vector2.RIGHT, 10.0, 256).is_empty())
	hit = world.circle_cast(Vector2(0,3), 1.0, Vector2.RIGHT, 10.0, 256)
	check("Tangent corner is detected", not hit.is_empty() and absf(hit.distance - 5.0) < 0.00001)
	var actor := OriginalActorMotion.new()
	actor.world = world
	actor.input_move = Vector2.RIGHT
	for frame in 300: actor.fixed_step()
	check("Sustained walking cannot cross wall", actor.position.x <= 5.0 - actor.radius and actor.position.x > 3.0)
	actor.position = Vector2(3.9,0)
	actor.velocity = Vector2.ZERO
	actor.input_move = Vector2(1,1).normalized()
	for frame in 12: actor.fixed_step()
	check("Oblique contact slides along wall", actor.position.x < 5.0 - actor.radius and actor.position.y > 0.1)
	var separated := world.separate_from_obstacles(Vector2(4.8,0), 1.0, 256)
	check("Overlap recovery projects out of wall", absf(separated.x-4.0)<0.00001)
	var arena := OriginalCollisionWorld.new()
	var attacker := OriginalActorMotion.new()
	var defender := OriginalActorMotion.new()
	attacker.world = arena
	defender.world = arena
	attacker.position = Vector2(-2,0)
	defender.position = Vector2(2,0)
	attacker.input_move = Vector2.RIGHT
	arena.register_actor(attacker)
	arena.register_actor(defender)
	attacker.dash()
	var was_stunned := false
	for frame in 90:
		attacker.fixed_step()
		defender.fixed_step()
		was_stunned = was_stunned or defender.is_stunned
		attacker.frame_step(attacker.fixed_delta)
		defender.frame_step(defender.fixed_delta)
	check("Dash contact pushes and stuns the other actor", was_stunned and defender.position.x > 3.0)
	check("Stun expires through frame updates", not defender.is_stunned)
	check("Player collision does not cross actor order", attacker.position.x < defender.position.x)
	var passed := true
	for result in results: passed = passed and result.passed
	var report := {"passed": passed,"cases": results,"native_unity_query_parity_verified": false,
		"scope": "Geometric boundary cases and locomotion/query integration"}
	var file := FileAccess.open("res://../evidence/godot-collision-check.json", FileAccess.WRITE)
	file.store_string(JSON.stringify(report, "\t") + "\n")
	file.close()
	print(JSON.stringify(report))
	quit(0 if passed else 1)
