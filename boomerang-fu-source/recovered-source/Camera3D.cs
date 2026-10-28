using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.PostProcessing;

public class Camera3D : Singleton<Camera3D>
{
	public Camera gameCamera;

	public Camera uiCamera;

	public Camera xrayCamera;

	public PostProcessingBehaviour uiCameraPostProcessing;

	public Transform focusPoint;

	private Bounds focusBoundary;

	private Transform cameraTransform;

	public Vector3 cameraAngle = new Vector3(0f, -0.4f, -0.7f);

	[SerializeField]
	private List<Camera3DTarget> cameraTargets = new List<Camera3DTarget>();

	private Vector3 focusTargetPos;

	private float moveSpeed = 5f;

	private float zoomInSpeed = 2f;

	private float zoomOutSpeed = 6f;

	private float zoomFactor = 1f;

	public float minimumZoom = 75f;

	public float maximumZoom = 350f;

	public float zoomYOffset = 0.015f;

	public float currentZoom;

	private float targetZoom;

	private Vector3 zoomPadding = new Vector3(15f, 15f, 0f);

	private bool aboutToZoomInOnKill;

	private bool zoomingInOnKill;

	private float steadicamSpeed = 0.33f;

	private float steadicamStrength = 1f;

	private float steadicamNoiseOffset;

	private bool steadicamNoiseReverse;

	private float shakeIntensity;

	private float shakeTimer;

	private float shakeDuration;

	public GameObject rainPFX;

	public LayerMask transparentFXLayer;

	private void Start()
	{
		gameCamera = GetComponent<Camera>();
		cameraTransform = base.transform;
		currentZoom = 0f;
		TurnOffRain();
	}

	public void Reset()
	{
		shakeIntensity = 0f;
		shakeTimer = 0f;
		aboutToZoomInOnKill = false;
		zoomingInOnKill = false;
		if (Singleton<GameManager>.Instance.testMode)
		{
			return;
		}
		cameraTargets.Clear();
		if (Singleton<LevelManager>.Instance.level != null)
		{
			float num = 15f;
			if (Mathf.Abs(Singleton<LevelManager>.Instance.level.transform.rotation.z) > 0.1f)
			{
				num = 0f;
			}
			if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
			{
				num += 2f;
			}
			maximumZoom = GetZoomDistanceFromBox(Singleton<LevelManager>.Instance.level.box.bounds.size.x + num, Singleton<LevelManager>.Instance.level.box.bounds.size.y + num);
		}
		currentZoom = maximumZoom;
		focusPoint.position = Vector3.zero;
		focusBoundary = new Bounds(Vector3.zero, Singleton<LevelManager>.Instance.level.box.size);
		for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
		{
			AddCameraTarget(Singleton<GameManager>.Instance.players[i].transform, 1f, 1f);
		}
		ResetFOV();
		if (QualitySettings.GetQualityLevel() == 0 || Singleton<SettingsManager>.Instance.currentPlatform == SettingsManager.Platform.Switch)
		{
			gameCamera.cullingMask &= ~(int)transparentFXLayer;
		}
		else
		{
			gameCamera.cullingMask |= transparentFXLayer;
		}
	}

	public void ResetAndShowOffPlayerOne()
	{
		shakeIntensity = 0f;
		shakeTimer = 0f;
		aboutToZoomInOnKill = false;
		zoomingInOnKill = false;
		if (!Singleton<GameManager>.Instance.testMode)
		{
			cameraTargets.Clear();
			currentZoom = minimumZoom;
			focusPoint.position = Vector3.zero;
			focusBoundary = new Bounds(Vector3.zero, Singleton<LevelManager>.Instance.level.box.size);
			AddCameraTarget(Singleton<GameManager>.Instance.players[0].transform, 1f, 1f);
		}
	}

	public void AddAllSceneryToCamera()
	{
		for (int i = 0; i < Singleton<LevelManager>.Instance.level.allMovingObstacles.Length; i++)
		{
			AddCameraTarget(Singleton<LevelManager>.Instance.level.allMovingObstacles[i].transform, 0.25f, 2f);
		}
		for (int j = 0; j < Singleton<LevelManager>.Instance.level.allMovingFloors.Length; j++)
		{
			AddCameraTarget(Singleton<LevelManager>.Instance.level.allMovingFloors[j].transform, 0.5f, 2f);
		}
		for (int k = 0; k < Singleton<LevelManager>.Instance.level.allTeleportals.Length; k++)
		{
			AddCameraTarget(Singleton<LevelManager>.Instance.level.allTeleportals[k].transform, 0.75f, 2f);
		}
		for (int l = 0; l < Singleton<LevelManager>.Instance.level.allSwitches.Length; l++)
		{
			AddCameraTarget(Singleton<LevelManager>.Instance.level.allSwitches[l].transform, 0.75f, 2f);
		}
		for (int m = 0; m < Singleton<LevelManager>.Instance.level.allCameraMagnets.Length; m++)
		{
			AddCameraTarget(Singleton<LevelManager>.Instance.level.allCameraMagnets[m].transform, Singleton<LevelManager>.Instance.level.allCameraMagnets[m].cameraMagnetism, 2f);
		}
	}

	private void Update()
	{
		if (!Singleton<SettingsManager>.Instance.trailerMode)
		{
			return;
		}
		if (Input.GetKey(KeyCode.Equals))
		{
			if (maximumZoom > currentZoom)
			{
				maximumZoom = currentZoom;
			}
			maximumZoom -= 10f;
		}
		if (Input.GetKey(KeyCode.Minus))
		{
			maximumZoom += 10f;
		}
	}

	private void LateUpdate()
	{
		if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Menu && Singleton<GameManager>.Instance.gameState != GameManager.GameState.Paused && Singleton<GameManager>.Instance.gameState != GameManager.GameState.Loading && !Singleton<GameManager>.Instance.testMode && !Singleton<TimeKeeper>.Instance.freezeFrame)
		{
			UpdateTargetTransforms();
			focusTargetPos = WeightedTargetCentroid() + new Vector3(0f, 0f, -1f);
			moveSpeed = 5f;
			focusPoint.position = Vector3.Lerp(focusPoint.position, focusTargetPos, Time.unscaledDeltaTime * moveSpeed);
			float num = 1f;
			cameraTransform.position = new Vector3(focusPoint.position.x * num, focusPoint.position.y, focusPoint.position.z * num);
			CalculateZoomDistance();
			cameraTransform.position += cameraAngle * zoomFactor * currentZoom;
			cameraTransform.LookAt(focusPoint.position * num);
			cameraTransform.position += new Vector3(0f, (0f - currentZoom) * zoomYOffset, 0f);
			cameraTransform.position += GetSteadiCamWiggle();
			cameraTransform.position += GetCameraShake();
			if (rainPFX.activeSelf)
			{
				rainPFX.transform.position = new Vector3(cameraTransform.position.x, rainPFX.transform.position.y, rainPFX.transform.position.z);
			}
		}
	}

	private void UpdateTargetTransforms()
	{
		for (int i = 0; i < cameraTargets.Count; i++)
		{
			if (cameraTargets[i].Transform != null)
			{
				cameraTargets[i].LastPosition = cameraTargets[i].Transform.position;
			}
			if (!cameraTargets[i].Transitioning)
			{
				cameraTargets[i].CameraPosition = cameraTargets[i].LastPosition;
			}
		}
	}

	private Vector3 WeightedTargetCentroid()
	{
		if (cameraTargets.Count < 1 || Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			return Vector3.zero;
		}
		Vector3 vector = Vector3.Lerp(focusPoint.transform.position, cameraTargets[0].CameraPosition, cameraTargets[0].Influence);
		Vector3 vector2 = Vector3.Lerp(focusPoint.transform.position, cameraTargets[0].CameraPosition, cameraTargets[0].Influence);
		for (int i = 1; i < cameraTargets.Count; i++)
		{
			Vector3 vector3 = Vector3.Lerp(focusPoint.transform.position, cameraTargets[i].CameraPosition, cameraTargets[i].Influence);
			if (vector3.x < vector.x)
			{
				vector.x = vector3.x;
			}
			if (vector3.x > vector2.x)
			{
				vector2.x = vector3.x;
			}
			if (vector3.y < vector.y)
			{
				vector.y = vector3.y;
			}
			if (vector3.y > vector2.y)
			{
				vector2.y = vector3.y;
			}
			if (vector3.z < vector.z)
			{
				vector.z = vector3.z;
			}
			if (vector3.z > vector2.z)
			{
				vector2.z = vector3.z;
			}
		}
		Vector3 vector4 = vector + 0.5f * (vector2 - vector);
		if (Singleton<LevelManager>.Instance.level != null)
		{
			vector4 = VectorExt.RotatePointAroundPivot(vector4, Vector3.zero, -Singleton<LevelManager>.Instance.level.box.transform.eulerAngles);
			vector4 = focusBoundary.ClosestPoint(vector4);
			vector4 = VectorExt.RotatePointAroundPivot(vector4, Vector3.zero, Singleton<LevelManager>.Instance.level.box.transform.eulerAngles);
		}
		return vector4;
	}

	private void CalculateZoomDistance()
	{
		if (Singleton<SettingsManager>.Instance.matchType == SettingsManager.MatchType.HideAndSeek)
		{
			currentZoom = maximumZoom;
			return;
		}
		Bounds bounds = new Bounds(focusPoint.position, Vector3.zero);
		for (int i = 0; i < cameraTargets.Count; i++)
		{
			Bounds bounds2 = new Bounds(Vector3.Lerp(focusPoint.transform.position, cameraTargets[i].CameraPosition, cameraTargets[i].Influence), (cameraTargets[i].Size * 2f + zoomPadding * (zoomingInOnKill ? 1f : 1f)) * cameraTargets[i].Influence);
			bounds.Encapsulate(bounds2);
		}
		if (Singleton<LevelManager>.Instance.levelBoundary != null && Singleton<LevelManager>.Instance.isLevelBoundaryShrinking)
		{
			bounds.Encapsulate(Singleton<LevelManager>.Instance.levelBoundary.bounds);
		}
		bounds.size += new Vector3(8f * gameCamera.aspect, 10f, 0f);
		targetZoom = GetZoomDistanceFromBox(bounds.size.x, bounds.size.y);
		bool flag = targetZoom > maximumZoom;
		targetZoom = Mathf.Clamp(targetZoom, minimumZoom, maximumZoom);
		Vector3 vector = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, 0f);
		Vector3 vector2 = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y, 0f);
		Vector3 vector3 = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y, 0f);
		Vector3 vector4 = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y, 0f);
		Debug.DrawLine(vector, vector2, flag ? Color.red : Color.white);
		Debug.DrawLine(vector2, vector3, flag ? Color.red : Color.white);
		Debug.DrawLine(vector3, vector4, flag ? Color.red : Color.white);
		Debug.DrawLine(vector4, vector, flag ? Color.red : Color.white);
		float num = zoomOutSpeed;
		if (currentZoom > targetZoom)
		{
			num = (zoomingInOnKill ? zoomInSpeed : zoomInSpeed);
		}
		currentZoom = Mathf.Lerp(currentZoom, targetZoom, num * Time.unscaledDeltaTime);
	}

	private float GetZoomDistanceFromBox(float width, float height)
	{
		float a = width / gameCamera.aspect * 0.5f / Mathf.Tan(gameCamera.fieldOfView * 0.5f * (MathF.PI / 180f));
		float b = height * 0.5f / Mathf.Tan(gameCamera.fieldOfView * 0.5f * (MathF.PI / 180f));
		return Mathf.Max(a, b);
	}

	private Vector3 GetSteadiCamWiggle()
	{
		if (steadicamNoiseOffset < 1000f && !steadicamNoiseReverse)
		{
			steadicamNoiseOffset += Time.unscaledDeltaTime * steadicamSpeed;
		}
		else if (!steadicamNoiseReverse)
		{
			steadicamNoiseReverse = true;
		}
		else if (steadicamNoiseOffset > 0f)
		{
			steadicamNoiseOffset -= Time.unscaledDeltaTime * steadicamSpeed;
		}
		else
		{
			steadicamNoiseOffset = 0f;
			steadicamNoiseReverse = false;
		}
		float x = Mathf.PerlinNoise(steadicamNoiseOffset, 0f) - 0.5f;
		float y = Mathf.PerlinNoise(0f, steadicamNoiseOffset) - 0.5f;
		return new Vector3(x, y, 0f) * steadicamStrength;
	}

	private Vector3 GetCameraShake()
	{
		if (!Singleton<SettingsManager>.Instance.GameOptions.screenShakeEnabled)
		{
			return Vector3.zero;
		}
		if (shakeTimer < shakeDuration && !Singleton<TimeKeeper>.Instance.freezeFrame)
		{
			shakeTimer += Time.unscaledDeltaTime;
			Vector3 result = UnityEngine.Random.insideUnitSphere * Mathf.Lerp(shakeIntensity, 0f, Easing.ExpoOut(shakeTimer / shakeDuration));
			result.Scale(new Vector3(0.33f, 0.33f, 1f));
			return result;
		}
		return Vector3.zero;
	}

	public void Shake(float intensity, float duration)
	{
		shakeIntensity = intensity;
		shakeDuration = duration * 3f;
		shakeTimer = 0f;
	}

	public Camera3DTarget AddCameraTarget(Transform targetTransform, float targetInfluence, float duration = 0.25f)
	{
		if (Singleton<GameManager>.Instance.testMode)
		{
			return null;
		}
		if ((aboutToZoomInOnKill || zoomingInOnKill) && targetInfluence < 1f)
		{
			return null;
		}
		Camera3DTarget camera3DTarget = new Camera3DTarget
		{
			Transform = targetTransform,
			Influence = targetInfluence,
			Size = (targetTransform.GetComponent<Collider2D>() ? (targetTransform.GetComponent<Collider2D>().bounds.size * targetInfluence) : Vector3.zero)
		};
		cameraTargets.Add(camera3DTarget);
		if (duration > 0f)
		{
			camera3DTarget.Transitioning = true;
			StartCoroutine(AddTargetLerp(camera3DTarget, targetInfluence, duration));
		}
		return camera3DTarget;
	}

	public void RemoveCameraTarget(Transform targetTransform, float duration = 1.5f)
	{
		if (Singleton<GameManager>.Instance.testMode)
		{
			return;
		}
		if (targetTransform == null)
		{
			Debug.LogWarning("tried to remove a transform that's been destroyed");
		}
		else
		{
			if ((aboutToZoomInOnKill || zoomingInOnKill) && !targetTransform.CompareTag("Player"))
			{
				return;
			}
			for (int i = 0; i < cameraTargets.Count; i++)
			{
				if (cameraTargets[i].Transform.GetInstanceID() == targetTransform.GetInstanceID())
				{
					if (duration > 0f)
					{
						cameraTargets[i].LastPosition = cameraTargets[i].Transform.position;
						cameraTargets[i].Transitioning = true;
						StartCoroutine(RemoveTargetLerp(cameraTargets[i], duration, removeAtZero: true));
					}
					else
					{
						cameraTargets.Remove(cameraTargets[i]);
					}
				}
			}
		}
	}

	public Camera3DTarget GetCameraTarget(Transform targetTransform)
	{
		if (Singleton<GameManager>.Instance.testMode)
		{
			return null;
		}
		for (int i = 0; i < cameraTargets.Count; i++)
		{
			if (cameraTargets[i].Transform.GetInstanceID() == targetTransform.GetInstanceID())
			{
				return cameraTargets[i];
			}
		}
		return null;
	}

	public void SetCameraTargetInfluence(Transform targetTransform, float targetInfluence, float duration = 2f)
	{
		if (Singleton<GameManager>.Instance.testMode)
		{
			return;
		}
		for (int i = 0; i < cameraTargets.Count; i++)
		{
			if (cameraTargets[i].Transform.GetInstanceID() == targetTransform.GetInstanceID())
			{
				StartCoroutine(SetTargetInfluenceLerp(cameraTargets[i], targetInfluence, duration));
				break;
			}
		}
	}

	public void ZoomInOnKill(Transform targetTransform)
	{
		if (!Singleton<SettingsManager>.Instance.trailerMode && !zoomingInOnKill && !aboutToZoomInOnKill && Singleton<GameManager>.Instance.activeExplosives.Count == 0 && !Singleton<GameManager>.Instance.AreAnyObstaclesMoving())
		{
			aboutToZoomInOnKill = true;
			if (currentZoom > minimumZoom * 1.1f)
			{
				StartCoroutine(ZoomInOnKillDelayed(targetTransform));
			}
		}
	}

	private IEnumerator ZoomInOnKillDelayed(Transform targetTransform)
	{
		List<Transform> transformsToRemove = new List<Transform>();
		for (int i = 0; i < cameraTargets.Count; i++)
		{
			if (cameraTargets[i].Transform == null)
			{
				transformsToRemove.Add(cameraTargets[i].Transform);
			}
			else if (cameraTargets[i].Transform.GetInstanceID() != targetTransform.GetInstanceID() && !cameraTargets[i].Transform.CompareTag("Player"))
			{
				transformsToRemove.Add(cameraTargets[i].Transform);
			}
		}
		yield return new WaitForSeconds(0.3f);
		aboutToZoomInOnKill = false;
		zoomingInOnKill = true;
		for (int j = 0; j < transformsToRemove.Count; j++)
		{
			RemoveCameraTarget(transformsToRemove[j], 0.3f);
		}
	}

	public void TurnOnRain()
	{
		rainPFX.SetActive(value: true);
	}

	public void TurnOffRain()
	{
		rainPFX.SetActive(value: false);
	}

	private IEnumerator SetTargetInfluenceLerp(Camera3DTarget cameraTarget, float targetInfluence, float duration)
	{
		float startInfluence = cameraTarget.Influence;
		float t = 0f;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			cameraTarget.Influence = Mathf.Lerp(startInfluence, targetInfluence, Easing.EaseOut(t / duration));
			yield return null;
		}
		cameraTarget.Influence = targetInfluence;
	}

	private IEnumerator AddTargetLerp(Camera3DTarget cameraTarget, float influence, float duration, bool removeAtZero = false)
	{
		Vector2 startPos = focusPoint.position;
		float t = 0f;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			cameraTarget.CameraPosition = Vector2.Lerp(startPos, cameraTarget.LastPosition, Easing.EaseOut(t / duration));
			if (influence < 1f && (zoomingInOnKill || aboutToZoomInOnKill))
			{
				cameraTarget.Transitioning = false;
				yield break;
			}
			yield return null;
		}
		cameraTarget.Transitioning = false;
	}

	private IEnumerator RemoveTargetLerp(Camera3DTarget cameraTarget, float duration, bool removeAtZero = false)
	{
		float t = 0f;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			cameraTarget.CameraPosition = Vector2.Lerp(cameraTarget.LastPosition, focusPoint.position, Easing.EaseOut(t / duration));
			yield return null;
		}
		cameraTarget.Transitioning = false;
		cameraTargets.Remove(cameraTarget);
	}

	public void LerpFOVFrom(float target, float duration)
	{
		gameCamera.DOComplete();
		gameCamera.fieldOfView = 16f;
		gameCamera.DOFieldOfView(target, duration).From(isRelative: false);
	}

	public void LerpFOVTo(float target, float duration)
	{
		gameCamera.DOFieldOfView(target, duration);
	}

	public void ResetFOV(float duration = 0f)
	{
		if (duration > 0f)
		{
			gameCamera.DOFieldOfView(16f, duration);
		}
		else
		{
			gameCamera.fieldOfView = 16f;
		}
	}
}
