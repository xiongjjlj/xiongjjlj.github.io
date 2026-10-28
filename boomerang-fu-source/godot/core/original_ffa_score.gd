class_name OriginalFfaScore
extends RefCounted
## Scoring decisions from GameManager.cs: DeathMatch + Kills + teamMatch=false.
## World death attribution, coroutine timing, audio and round UI remain outside
## this state component; a score event alone must not fabricate those effects.

enum ActorState { ALIVE, DEAD, FALLING }
enum Decision { CHECK_SURVIVORS, END_MATCH }
var scores: Dictionary = {}
var actors: Dictionary = {}
var winners: Array[int] = []
var goal := 10
var tie_breaker := false
var tutorial := false

func _init(player_ids: Array = [], score_goal := 10) -> void:
	goal = score_goal
	for id in player_ids:
		scores[int(id)] = {"points": 0, "suicides": 0, "team_kills": 0, "total": 0, "rank": 0}
		actors[int(id)] = {"state": ActorState.ALIVE, "fire": false, "frozen": false}

func net_score(id: int) -> int:
	var score: Dictionary = scores[id]
	return int(score.points) - int(score.suicides) - int(score.team_kills)

func add_player_kill(id: int, killer_was_opponent := true) -> Decision:
	if tutorial: return Decision.CHECK_SURVIVORS
	if not killer_was_opponent:
		scores[id].team_kills += 1
		return Decision.CHECK_SURVIVORS
	scores[id].points += 1
	if tie_breaker:
		winners.assign([id])
		return Decision.END_MATCH
	if net_score(id) >= goal and not winners.has(id):
		winners.append(id)
		var actor: Dictionary = actors[id]
		if winners.size() == 1 and not another_player_could_win_this_round(id) and not actor.fire and not actor.frozen and actor.state == ActorState.ALIVE:
			return Decision.END_MATCH
	return Decision.CHECK_SURVIVORS

func add_player_suicide(id: int) -> void:
	if tutorial or tie_breaker: return
	scores[id].suicides += 1
	if winners.has(id) and net_score(id) < goal:
		winners.erase(id)

func player_could_win_this_round(id: int) -> bool:
	if actors[id].state == ActorState.DEAD: return false
	var remaining := 0
	for other in actors:
		if other != id and actors[other].state != ActorState.DEAD:
			remaining += 1
	return net_score(id) + remaining >= goal

func another_player_could_win_this_round(id: int) -> bool:
	for other in actors:
		if other != id and player_could_win_this_round(other): return true
	return false

func prevent_negative_scores() -> void:
	# The original performs this at round end, not immediately on a suicide.
	for score in scores.values():
		while score.points < score.suicides + score.team_kills:
			if score.team_kills > 0: score.team_kills -= 1
			elif score.suicides > 0: score.suicides -= 1

func remove_losing_match_winners() -> void:
	var highest := 0
	for id in actors:
		if winners.has(id): highest = maxi(highest, net_score(id))
	for id in actors:
		if winners.has(id) and net_score(id) < highest: winners.erase(id)

func end_round_scores() -> void:
	prevent_negative_scores()
	if winners.size() > 1: remove_losing_match_winners()
