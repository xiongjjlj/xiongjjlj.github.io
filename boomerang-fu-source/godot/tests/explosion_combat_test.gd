extends SceneTree
const C = preload("res://core/original_timekeeper.gd")
var differences:Array=[]
func compare(a:Variant,b:Variant,path:String)->void:
	if a is Dictionary:
		for key in a: compare(a[key],b[key],path+"."+key)
		return
	if a is Array:
		for i in a.size(): compare(a[i],b[i],path+"[%d]"%i)
		return
	if a is float:b=C.f32(b)
	if a!=b:differences.append({"field":path,"actual":a,"expected":b})
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/explosive-native-lifecycle.json"))
	for item in fixture.combat:
		var world:=OriginalCollisionWorld.new()
		var actors:Array[OriginalActorMotion]=[]
		var weapons:Array[OriginalPlayerWeapons]=[]
		for i in 2:
			var actor:=OriginalActorMotion.new()
			actor.world=world
			actors.append(actor)
			var weapon:=OriginalPlayerWeapons.new()
			weapon.initialize(actor,world)
			weapons.append(weapon)
		var combat:=OriginalCombatWorld.new()
		combat.initialize(actors,weapons,world)
		# Native Actor-trigger fixture injects null killer; Player.Init defaults to self.
		# The player default/expiry and boundary override have their own native fixture.
		combat.killer_ids[1]=-1
		var victim:OriginalActorMotion=actors[1]
		victim.position=Vector2(item.initial.position[0],item.initial.position[1])
		victim.is_dashing=item.initial.dash
		victim.is_invulnerable=item.initial.invulnerable
		victim.is_shielded=item.initial.shield
		victim.time_scaler=C.f32(item.initial.scale)
		victim.velocity=Vector2(3,4)
		weapons[1].powers.shield_hits=int(item.initial.hits)
		var blast:=OriginalExplosion.new()
		blast.position=Vector2.ZERO
		blast.source_z=C.f32(-2.75)
		blast.owner=1 if item.initial.self else 0
		for i in (2 if item.name=="repeat" else 1):
			combat.explosion_shield(1,blast)
			combat.explosion_damage(1,blast)
		var push:=Vector2.ZERO
		var duration:=0.0
		var stun:=0.0
		var deaths:=0
		for event in combat.events:
			if event.type=="death":deaths+=1
			if event.type=="explosion_shield":
				push=event.direction
				duration=event.duration
				stun=event.stun
		var actual:Dictionary={"dead":victim.state==OriginalActorMotion.State.DEAD,"deaths":deaths,"shield":victim.is_shielded,"hits":weapons[1].powers.shield_hits,"invulnerable":victim.is_invulnerable,"dodges":victim.explosion_dodges,"burnt":victim.is_burnt,"killer":combat.killer_ids[1],"killer_duration":combat.killer_timers.get(1,0.0),"push":[push.x,push.y],"push_duration":duration,"push_lerp":stun}
		# Native fixture injects Die and does not run the already-tested power cleanup.
		if actual.dead: actual.erase("hits")
		compare(actual,item.result,item.name)
	var report:Dictionary={"passed":differences.is_empty(),"cases":fixture.combat.size(),"differences":differences,"scope":"Original damage/shield methods with the native fixture's injected null initial killer; pushback request compared. Native adapter records pushback instead of simulating subsequent actor velocity; shield counter after injected death is outside this comparison.","full_game_parity_verified":false}
	FileAccess.open("res://../evidence/godot-native-explosion-combat-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_EXPLOSION_COMBAT_TEST "+JSON.stringify(report))
	quit(0 if report.passed else 1)
