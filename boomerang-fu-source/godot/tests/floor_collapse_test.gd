extends SceneTree
var differences:Array=[]
var comparisons:=0
var maximum_error:=0.0
func compare(a:Variant,b:Variant,label:String)->void:
	if b is Array:
		compare(a.size(),b.size(),label+"/size")
		for i in mini(a.size(),b.size()):compare(a[i],int(b[i]),label+"/"+str(i))
		return
	comparisons+=1
	var bad:bool=a!=b
	if a is float:
		var error:=absf(a-OriginalTimekeeper.f32(b));maximum_error=maxf(maximum_error,error);bad=error>0.0
	if bad and differences.size()<30:differences.append({"label":label,"actual":a,"expected":b})
func _initialize()->void:call_deferred("run")
func run()->void:
	var data:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/floor-hazards-native.json"))
	for number in data.collapse.size():
		var c:Dictionary=data.collapse[number];var manager:=OriginalFloorHazards.new();manager.rng=OriginalUnityRandom.new(c.seed)
		manager.settings={"collapsing":true,"duration":c.duration,"box_size":{"x":60.0,"y":54.0},"falling_order":range(int(c.count))}
		for i in int(c.count):
			var node:=Node3D.new();root.add_child(node);node.position=Vector3(c.positions[i][0],c.positions[i][1],c.positions[i][2])
			var h:=OriginalFloorHazard.new();h.initialize(false,node.position,Color(1,1,1,0),manager.rng)
			manager.floors[i]={"id":i,"object_id":i,"node":node,"model":h}
		manager.initialize_playlist()
		compare(manager.playlist,c.order,str(number)+"/order")
		for sample in c.samples:
			manager.events.clear();manager.level_step(sample.playing,int(sample.opponents),c.dt)
			var activated:=-1
			for event in manager.events:
				if event.type=="tag":activated=event.floor
			var label:=str(number)+"/"+str(sample.frame)
			compare(manager.collapsing,sample.collapsing,label+"/collapsing")
			compare(manager.countdown,sample.timer,label+"/timer")
			compare(manager.playlist.size(),int(sample.count),label+"/count")
			compare(activated,int(sample.activated),label+"/activated")
		for item in manager.floors.values():item.node.free()
	var report:={"passed":differences.is_empty(),"cases":data.collapse.size(),"comparisons":comparisons,"maximum_error":maximum_error,"differences":differences}
	FileAccess.open("res://../evidence/godot-floor-collapse-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  "))
	print("BF_FLOOR_COLLAPSE "+JSON.stringify(report));quit(0 if report.passed else 1)
