class_name OriginalFfaRound
extends RefCounted
## DeathMatch/Kills/FFA score and survivor-coroutine decisions. Original menus,
## wipe transitions, map playlist and match-end presentation are separate work.
const C = preload("res://core/original_timekeeper.gd")
enum Phase { PLAYING, WAIT_INITIAL, WAIT_SURVIVORS, SCORES, MATCH_END }
var phase := Phase.PLAYING
var scores: OriginalFfaScore
var actors: Array[OriginalActorMotion] = []
var round_number := 1
var wait_time := 0.0
var defer_frame := false

func initialize(motions: Array[OriginalActorMotion], reset_scores := false) -> void:
	actors = motions
	if scores == null or reset_scores:
		# SettingsManager.PrepareMatchLength: default Medium, two players => 14.
		scores = OriginalFfaScore.new(range(actors.size()), 14 if actors.size() < 3 else 16)
		round_number = 1
	phase = Phase.PLAYING
	wait_time = 0.0
	defer_frame = false
	sync_scores()

func sync_scores() -> void:
	for index in actors.size():
		scores.actors[index].state = actors[index].state
		scores.actors[index].frozen = actors[index].is_frozen

func simulation_running() -> bool:
	return phase in [Phase.PLAYING, Phase.WAIT_INITIAL, Phase.WAIT_SURVIVORS]

func on_death(victim: int, killer: int, cause: String) -> void:
	sync_scores()
	if killer >= 0 and killer != victim:
		if scores.add_player_kill(killer) == OriginalFfaScore.Decision.END_MATCH:
			phase = Phase.MATCH_END
			return
	elif killer == victim and cause != "fall": scores.add_player_suicide(victim)
	# A new death stops and restarts CheckPlayersLeftStanding.
	phase = Phase.WAIT_INITIAL
	wait_time = C.f32(0.15)
	defer_frame = true

func frame_step(delta: float) -> void:
	if defer_frame:
		defer_frame = false
		return
	if phase not in [Phase.WAIT_INITIAL, Phase.WAIT_SURVIVORS]: return
	wait_time = C.f32(wait_time - C.f32(delta))
	if wait_time > 0.0: return
	if phase == Phase.WAIT_INITIAL:
		phase = Phase.WAIT_SURVIVORS
		wait_time = C.f32(1.3)
		return
	sync_scores()
	var survivors := 0
	for actor in actors:
		if actor.state != OriginalActorMotion.State.DEAD: survivors += 1
	if survivors > 1:
		phase = Phase.PLAYING
		return
	scores.end_round_scores()
	phase = Phase.SCORES
