using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuInstructions : UIMenuBase
{
	private CanvasGroup canvasGroup;

	public HorizontalLayoutGroup instructionsLayoutGroup;

	public InstructionPanel[] instructionPanels;

	public GameObject instructionsTitleGeneric;

	public TextMeshProUGUI instructionsTitleSpecific;

	private int panelIndex;

	private void Awake()
	{
		canvasGroup = GetComponent<CanvasGroup>();
	}

	public override void Init()
	{
		canvasGroup.alpha = 0f;
		canvasGroup.DOFade(1f, 0.15f);
		base.Init();
		Singleton<UIManager>.Instance.ZoomFrom(0.9f, 0.15f);
		Singleton<Camera3D>.Instance.LerpFOVTo(17.5f, 0.15f);
	}

	public void ShowInstructionPanels(Player player = null)
	{
		Singleton<GameManager>.Instance.Pause();
		Singleton<CameraFX>.Instance.Blur();
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/roundtitle_other");
		if (player == null)
		{
			instructionsTitleGeneric.SetActive(value: true);
			instructionsTitleSpecific.gameObject.SetActive(value: false);
			panelIndex = 0;
			HashSet<Guid> hashSet = new HashSet<Guid>();
			for (int i = 0; i < Singleton<GameManager>.Instance.players.Count; i++)
			{
				RewiredController component = Singleton<GameManager>.Instance.players[i].GetComponent<RewiredController>();
				if (component == null)
				{
					continue;
				}
				if (Singleton<GameManager>.Instance.DoesPlayerHaveValidJoystick(component.rewiredPlayer))
				{
					if (hashSet.Contains(component.rewiredPlayer.controllers.Joysticks[0].hardwareTypeGuid))
					{
						continue;
					}
					hashSet.Add(component.rewiredPlayer.controllers.Joysticks[0].hardwareTypeGuid);
				}
				else if (Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.PC)
				{
					UIResponse_Continue();
					break;
				}
				instructionPanels[panelIndex].Show(Singleton<GameManager>.Instance.players[i].GetComponent<RewiredController>().rewiredPlayer, (float)panelIndex * 0.1f);
				panelIndex++;
			}
			if (Singleton<SettingsManager>.Instance.currentPlatform != SettingsManager.Platform.PC && panelIndex == 0)
			{
				UIResponse_Continue();
			}
			if (panelIndex >= 6)
			{
				instructionsLayoutGroup.spacing = 10f;
			}
			else if (panelIndex == 5)
			{
				instructionsLayoutGroup.spacing = 20f;
			}
			else if (panelIndex == 4)
			{
				instructionsLayoutGroup.spacing = 40f;
			}
			else
			{
				instructionsLayoutGroup.spacing = 80f;
			}
		}
		else
		{
			RewiredController component2 = player.GetComponent<RewiredController>();
			if (component2 != null)
			{
				Singleton<UIManager>.Instance.ClearAllPlayersFromEventSystem();
				Singleton<UIManager>.Instance.AssignSpecificPlayerToEventSystem(component2.rewiredPlayer);
				Debug.Log("Assigned " + player.character.name + " to event system");
			}
			instructionsTitleGeneric.SetActive(value: false);
			instructionsTitleSpecific.gameObject.SetActive(value: true);
			instructionsTitleSpecific.SetText(LocalizationManager.GetTranslation(Singleton<GameManager>.Instance.characterNames[player.character.characterType]) + " " + LocalizationManager.GetTranslation("ui_suffix_joined"));
			instructionsTitleSpecific.color = player.character.Color;
			if (component2 != null)
			{
				instructionPanels[0].Show(component2.rewiredPlayer, 0.1f);
			}
		}
	}

	public void UIResponse_Continue()
	{
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = false;
		Singleton<UIManager>.Instance.AssignAllPlayersToEventSystem();
		Singleton<CameraFX>.Instance.UnBlur();
		canvasGroup.alpha = 1f;
		canvasGroup.DOFade(0f, 0.15f);
		Singleton<UIManager>.Instance.ZoomTo(1.111f, 0.15f);
		Singleton<Camera3D>.Instance.ResetFOV(0.15f);
		StartCoroutine(DelayedClose(0.15f));
	}

	private IEnumerator DelayedClose(float closeDelay)
	{
		float t = 0f;
		while (t < closeDelay)
		{
			t += Time.unscaledDeltaTime;
			yield return null;
		}
		Singleton<UIManager>.Instance.eventSystem.sendNavigationEvents = true;
		for (int i = 0; i < panelIndex; i++)
		{
			instructionPanels[i].Hide();
		}
		Singleton<GameManager>.Instance.Unpause();
		UIResponse_Close();
		Singleton<UIManager>.Instance.ZoomTo(1f, 0f);
	}
}
