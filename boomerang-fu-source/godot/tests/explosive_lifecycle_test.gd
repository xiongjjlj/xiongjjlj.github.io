extends SceneTree
const C = preload("res://core/original_timekeeper.gd")
var differences: Array = []
var mismatch_count := 0
func compare(actual: Variant, expected: Variant, path: String) -> void:
	if actual is Dictionary:
		for key in actual: compare(actual[key],expected[key],path+"/"+str(key))
	elif actual is Array:
		if actual.size()!=expected.size():
			mismatch_count+=1
			if differences.size()<30: differences.append({"path":path,"size":actual.size(),"expected_size":expected.size()})
		else:
			for i in actual.size(): compare(actual[i],expected[i],path+"/"+str(i))
	else:
		var reference: Variant = C.f32(expected) if actual is float else expected
		if actual != reference:
			mismatch_count += 1
			if differences.size()<30: differences.append({"path":path,"actual":actual,"expected":reference})
func weapon() -> OriginalPlayerWeapons:
	var w:=OriginalPlayerWeapons.new()
	w.initialize(OriginalActorMotion.new(),OriginalCollisionWorld.new())
	w.held_discs=0
	return w
func fuse_state(g:OriginalGrenade,w:OriginalPlayerWeapons,recycled:bool,power:int) -> Dictionary:
	return {"timer":g.timer,"rate":g.blink_rate,"blink_timer":g.blink_timer,"blink_on":g.blink_on,"counter":g.blink_counter,"active":g.active,"recycled":recycled,"explosions":1 if recycled else 0,"kind":("ExplosionFreeze" if power&1024 else ("ExplosionFire" if power&256 else "Explosion")) if recycled else "","pending":w.powers.pending_discs,"waits":w.respawn_jobs.map(func(job):return job.remaining)}
func _initialize() -> void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/explosive-native-lifecycle.json"))
	var frames:=0
	for item in fixture.fuses:
		var w:=weapon()
		if item.name=="dead_owner":w.actor.state=OriginalActorMotion.State.DEAD
		if item.name=="full_owner":w.held_discs=2
		if item.name=="pending_owner":w.powers.pending_discs=2
		var g:=OriginalGrenade.new()
		g.arm()
		g.timer=C.f32(item.initial.timer)
		g.blink_timer=C.f32(item.initial.blink_timer)
		g.blink_on=item.initial.blink_on
		var recycled:=false
		compare(fuse_state(g,w,recycled,int(item.power)),item.initial,item.name+"/initial")
		for frame in item.frames:
			if g.step(C.f32(frame.dt)):
				recycled=true
				if not item.mini:w.request_respawn(1.75)
			compare(fuse_state(g,w,recycled,int(item.power)),frame.state,item.name+"/"+str(frames))
			frames+=1
	for item in fixture.respawns:
		var w:=weapon()
		if item.name=="full":w.held_discs=2
		if item.name=="pending":w.powers.pending_discs=2
		if item.name=="dead_start":w.actor.state=OriginalActorMotion.State.DEAD
		w.request_respawn(1.75,2 if item.name=="two" else 1)
		if item.name=="overlap":w.request_respawn(3.25)
		compare({"held":w.held_discs,"pending":w.powers.pending_discs,"waits":w.respawn_jobs.map(func(job):return job.remaining)},item.initial,item.name+"/initial")
		if item.name=="dead_finish":w.actor.state=OriginalActorMotion.State.DEAD
		if item.name.begins_with("fall"):
			w.actor.state=OriginalActorMotion.State.FALLING
			w.actor.fall_timer=C.f32(0.2 if item.name=="fall_early" else 0.34)
		if item.name=="pickup_during":w.grant_replacement_disc()
		for job in w.respawn_jobs:w.finish_respawn(job.count)
		compare({"held":w.held_discs,"pending":w.powers.pending_discs,"pickups":w.replacement_pickups},item.result,item.name+"/finish")
	var report:Dictionary={"passed":mismatch_count==0,"cases":fixture.fuses.size()+fixture.respawns.size(),"frames":frames,"mismatch_count":mismatch_count,"differences":differences,"scope":"Fuse/blink timing and explicit inventory coroutine resumptions; blast curves/damage/rendering are separate"}
	FileAccess.open("res://../evidence/godot-native-explosive-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"\t")+"\n")
	print("BF_EXPLOSIVE_CHECK "+JSON.stringify(report))
	quit(0 if mismatch_count==0 else 1)
