extends SceneTree
const T=preload("res://core/original_player_ai_targeting.gd")
const C=preload("res://core/original_timekeeper.gd")
var differences:Array=[]
var compared:=0
var max_error:=0.0
func compare(got:Variant,want:Variant,path:String)->void:
	if want is Array:
		if got.size()!=want.size():differences.append({"path":path,"reason":"array size"});return
		for i in want.size():compare(got[i],want[i],path+"["+str(i)+"]")
	else:
		compared+=1
		var expected:float=float(want) if path.contains("rng") or path.contains(".id") else C.f32(want)
		var error:=absf(float(got)-expected);max_error=maxf(max_error,error)
		if error!=0 and differences.size()<60:differences.append({"path":path,"got":got,"expected":expected})
func snapshot_actor(raw:Dictionary)->Dictionary:
	var snapshot:=raw.duplicate(true)
	for key in ["position","forward","velocity"]:
		if snapshot.has(key):snapshot[key]=Vector3(snapshot[key][0],snapshot[key][1],snapshot[key][2])
	for key in ["since_teleport","speed","disguise_time","aim","aim_max"]:
		if snapshot.has(key):snapshot[key]=C.f32(snapshot[key])
	return snapshot
func _initialize()->void:
	var fixture:Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../evidence/player-ai-native-policy.json"))
	var selected_counts:={"block":0,"attack":0,"throw":0};var index:=0
	for row:Dictionary in fixture.selection:
		var targeting:=T.new();targeting.policy.difficulty=int(row.difficulty);targeting.policy.teleport_threshold=C.f32([1.5,0.75,0.25,0.0][int(row.difficulty)]);targeting.policy.rng.initialize(int(row.seed))
		targeting.line_of_sight=func(point:Vector3)->bool:return row.los_mode==0 or (row.los_mode==2 and point.x+point.y>row.los_threshold)
		var own:=snapshot_actor(row.self);var players:Array=[own];var decoys:Array=[];var discs:Array=[];var switches:Array=[];var by_id:Dictionary={}
		for item:Dictionary in row.players:
			var target:=snapshot_actor(item);players.append(target);by_id[int(target.id)]=target
		for item:Dictionary in row.decoys:
			var target:=snapshot_actor(item.actor);target.owner_exists=item.owner_exists;target.owner_self=item.owner_self;target.opponent=item.opponent;decoys.append(target);by_id[int(target.id)]=target
		for item:Dictionary in row.discs:discs.append(snapshot_actor(item))
		for item:Dictionary in row.switches:switches.append(snapshot_actor(item))
		var path:="selection["+str(index)+"]";index+=1
		var blocked:=targeting.best_block(own,players,discs);compare(blocked.get("id",-1),row.block,path+".block.id");compare(targeting.policy.rng.get_state(),row.after_block,path+".block.rng");selected_counts.block+=int(not blocked.is_empty())
		var attacked:=targeting.best_attack(own,players,decoys,switches,int(row.state));compare(attacked.get("id",-1),row.attack,path+".attack.id");compare(targeting.policy.rng.get_state(),row.after_attack,path+".attack.rng");selected_counts.attack+=int(not attacked.is_empty())
		var thrown:=targeting.best_throw(own,players,decoys);compare(thrown.get("id",-1),row.thrown,path+".throw.id");compare(targeting.policy.rng.get_state(),row.after_throw,path+".throw.rng");selected_counts.throw+=int(not thrown.is_empty())
		for prediction:Dictionary in row.predictions:
			var point:=targeting.target_after_throw(own,by_id[int(prediction.id)]);compare([point.x,point.y,point.z],prediction.position,path+".prediction["+str(prediction.id)+"]")
	var report:={"passed":differences.is_empty(),"comparisons":compared,"max_absolute_error":max_error,"tolerance":0.0,"differences":differences,"scenarios":fixture.selection.size(),"selected_counts":selected_counts,"full_player_ai_verified":false}
	FileAccess.open("res://../evidence/godot-player-ai-targeting-check.json",FileAccess.WRITE).store_string(JSON.stringify(report,"  ")+"\n")
	print("BF_PLAYER_AI_TARGETING "+JSON.stringify(report));quit(0 if differences.is_empty() else 1)
