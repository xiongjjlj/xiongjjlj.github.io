using System;
using DG.Tweening;
using FMOD.Studio;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuRoundTitles : UIMenuBase
{
	public enum RoundTitleType
	{
		Ready,
		Go,
		WarmUp,
		TieBreaker,
		FirstKillWins,
		CharacterIsSeeker,
		CharacterCloseYourEyes,
		CharacterOpenYourEyes,
		Hide,
		Seek,
		HidersWin,
		CharacterWins
	}

	[Serializable]
	public class RoundTitleString
	{
		public RoundTitleType type;

		public LocalizedString localizedString;
	}

	public RectTransform titleRoot;

	public Canvas titleCanvas;

	public TextMeshProUGUI titleText;

	public Sequence titleSequence;

	public ParticleSystem goPFX;

	public bool titlesShowing;

	private float hideAndSeekFontSize = 100f;

	private EventInstance readySoundEvent;

	[SerializeField]
	public RoundTitleString[] roundTitleStrings;

	public override void Init()
	{
		base.Init();
		titleCanvas.enabled = false;
		titleSequence = null;
	}

	public override void Suspend()
	{
		base.Suspend();
		if (titleSequence != null)
		{
			titleSequence.Pause();
		}
	}

	public override void Resume()
	{
		base.Resume();
		if (titleSequence != null)
		{
			titleSequence.Play();
		}
	}

	public override void Cleanup()
	{
		base.Cleanup();
		if (titleSequence != null)
		{
			titleSequence.Kill();
		}
	}

	public void ShowRoundTitles()
	{
		if (titleSequence != null)
		{
			titleSequence.Kill();
		}
		titlesShowing = true;
		titleSequence = DOTween.Sequence();
		if (Singleton<LevelManager>.Instance.level != null && Singleton<LevelManager>.Instance.level.isTutorialLevel)
		{
			titleSequence.AppendCallback(delegate
			{
				ShowString(RoundTitleType.WarmUp);
			}).AppendInterval(1.2f).AppendCallback(HideString)
				.AppendInterval(0.3f);
		}
		else if (Singleton<SettingsManager>.Instance.tieBreaker)
		{
			titleSequence.AppendCallback(delegate
			{
				ShowString(RoundTitleType.TieBreaker);
			}).AppendInterval(1.5f).AppendCallback(delegate
			{
				ShowString(RoundTitleType.FirstKillWins, animateIn: false);
			})
				.AppendInterval(1.5f)
				.AppendCallback(HideString)
				.AppendInterval(0.3f);
		}
		titleSequence.AppendCallback(delegate
		{
			ShowString(RoundTitleType.Ready);
		}).AppendInterval(1f).AppendCallback(delegate
		{
			ShowString(RoundTitleType.Go);
			titlesShowing = false;
		})
			.AppendInterval(0.7f)
			.AppendCallback(HideString);
		titleSequence.OnComplete(OnTitleSequenceComplete);
	}

	public void ShowSeekerTitle()
	{
		Player seeker = Singleton<GameManager>.Instance.GetSeeker();
		if (titleSequence != null)
		{
			titleSequence.Kill();
		}
		titlesShowing = true;
		titleSequence = DOTween.Sequence();
		titleSequence.AppendCallback(delegate
		{
			ShowString(RoundTitleType.CharacterIsSeeker, animateIn: true, seeker.character.characterType, hideAndSeekFontSize);
			seeker.playerInput.StartVibration(1f, 0.2f);
		}).AppendInterval(1.2f).AppendCallback(delegate
		{
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/transition_out");
			StartCoroutine(Singleton<GameManager>.Instance.victoryCircleWipe.TransitionOut(resetFocusPosition: false, 0.8f));
		})
			.AppendInterval(0.2f)
			.AppendCallback(delegate
			{
				seeker.ShowSeekerPFX();
				Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/spawn_golden_boomerang", seeker.transform.position);
			})
			.AppendInterval(0.4f)
			.AppendCallback(delegate
			{
				HideString();
			})
			.AppendInterval(2f)
			.AppendCallback(delegate
			{
				ShowString(RoundTitleType.CharacterCloseYourEyes, animateIn: true, seeker.character.characterType, hideAndSeekFontSize);
				seeker.playerInput.SetVibration(0.1f);
				seeker.character.SetFacialExpression(FacialExpression.blinkcontent, 15f);
			})
			.AppendInterval(2f)
			.AppendCallback(delegate
			{
				HideString();
			});
		titleSequence.OnComplete(OnTitleSequenceComplete);
	}

	public void ShowHideTitle()
	{
		if (titleSequence != null)
		{
			titleSequence.Kill();
		}
		titlesShowing = true;
		titleSequence = DOTween.Sequence();
		titleSequence.AppendCallback(delegate
		{
			ShowString(RoundTitleType.Hide, animateIn: true, CharacterType.None, hideAndSeekFontSize);
		}).AppendInterval(1f).AppendCallback(delegate
		{
			titlesShowing = false;
		})
			.AppendInterval(1f)
			.AppendCallback(HideString);
		titleSequence.OnComplete(OnTitleSequenceComplete);
	}

	public void ShowSeekTitle()
	{
		Player seeker = Singleton<GameManager>.Instance.GetSeeker();
		if (titleSequence != null)
		{
			titleSequence.Kill();
		}
		titlesShowing = true;
		titleSequence = DOTween.Sequence();
		titleSequence.AppendCallback(delegate
		{
			ShowString(RoundTitleType.CharacterOpenYourEyes, animateIn: true, seeker.character.characterType, hideAndSeekFontSize);
			seeker.playerInput.StartVibration(1f, 0.2f);
			seeker.character.SetFacialExpression(FacialExpression.content, 1f);
			if (Singleton<SettingsManager>.Instance.limitSeekAttempts)
			{
				Singleton<GameManager>.Instance.seekAttemptBar.ShowSeekAttemptBar();
			}
		}).AppendInterval(1.8f).AppendCallback(delegate
		{
			titlesShowing = false;
		})
			.AppendInterval(0.2f)
			.AppendCallback(delegate
			{
				HideString();
			})
			.AppendInterval(1f)
			.AppendCallback(delegate
			{
				ShowString(RoundTitleType.Go);
			})
			.AppendInterval(0.7f)
			.AppendCallback(HideString);
		titleSequence.OnComplete(OnTitleSequenceComplete);
	}

	public void ShowSeekerWinsTitle()
	{
		Player seeker = Singleton<GameManager>.Instance.GetSeeker();
		if (titleSequence != null)
		{
			titleSequence.Kill();
		}
		titlesShowing = true;
		titleSequence = DOTween.Sequence();
		titleSequence.AppendCallback(delegate
		{
			ShowString(RoundTitleType.CharacterWins, animateIn: true, seeker.character.characterType, hideAndSeekFontSize);
		}).AppendInterval(2f).AppendCallback(delegate
		{
			HideString();
			titlesShowing = false;
		});
		titleSequence.OnComplete(OnTitleSequenceComplete);
	}

	public void ShowHidersWinTitle()
	{
		if (titleSequence != null)
		{
			titleSequence.Kill();
		}
		titlesShowing = true;
		titleSequence = DOTween.Sequence();
		Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/win_golden_boomerang");
		titleSequence.AppendCallback(delegate
		{
			ShowString(RoundTitleType.HidersWin, animateIn: true, CharacterType.None, hideAndSeekFontSize);
		}).AppendInterval(2f).AppendCallback(delegate
		{
			HideString();
			titlesShowing = false;
		});
		titleSequence.OnComplete(OnTitleSequenceComplete);
	}

	private void ShowString(RoundTitleType titleType, bool animateIn = true, CharacterType characterType = CharacterType.None, float fontSize = 140f)
	{
		titleCanvas.enabled = true;
		string text = GetString(titleType);
		if (characterType != CharacterType.None)
		{
			HSBColor hSBColor = HSBColor.FromColor(Singleton<ColorManager>.Instance.GetColorForCharacterType(characterType));
			hSBColor.b = 1f;
			hSBColor.s = Mathf.Min(hSBColor.s, 0.5f);
			text = string.Format(text, "<color=#" + ColorUtility.ToHtmlStringRGB(hSBColor.ToColor()) + ">" + LocalizationManager.GetTranslation(Singleton<GameManager>.Instance.characterNames[characterType]) + "</color>");
		}
		if (string.IsNullOrEmpty(text))
		{
			Debug.LogError(titleType.ToString() + " string is missing!");
			return;
		}
		titleText.fontSize = fontSize;
		titleText.text = text;
		if (titleType == RoundTitleType.Go)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(titleRoot);
		}
		if (animateIn)
		{
			titleRoot.localScale = Vector3.one;
			titleRoot.DOScale(0.5f, 0.15f).SetEase(Ease.OutBack).From(isRelative: false);
		}
		ParticleSystem.ShapeModule shape = goPFX.shape;
		switch (titleType)
		{
		case RoundTitleType.Go:
			if (readySoundEvent.isValid())
			{
				readySoundEvent.stop(STOP_MODE.ALLOWFADEOUT);
			}
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/roundtitle_go");
			shape.scale = new Vector3(titleRoot.rect.width / 42f, shape.scale.y, shape.scale.z);
			goPFX.Play();
			break;
		case RoundTitleType.Ready:
		case RoundTitleType.CharacterIsSeeker:
		case RoundTitleType.CharacterOpenYourEyes:
			readySoundEvent = Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/roundtitle_ready");
			break;
		case RoundTitleType.CharacterCloseYourEyes:
			readySoundEvent = Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/roundtitle_closeyoureyes");
			break;
		case RoundTitleType.WarmUp:
		case RoundTitleType.TieBreaker:
		case RoundTitleType.FirstKillWins:
		case RoundTitleType.HidersWin:
		case RoundTitleType.CharacterWins:
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/roundtitle_other");
			break;
		case RoundTitleType.Hide:
			Singleton<AudioManager>.Instance.PlayOneShot("event:/ui/show_alert");
			break;
		case RoundTitleType.Seek:
			break;
		}
	}

	private void HideString()
	{
		titleCanvas.enabled = false;
	}

	private void OnTitleSequenceComplete()
	{
		titlesShowing = false;
		UIResponse_Close();
	}

	private string GetString(RoundTitleType titleType)
	{
		for (int i = 0; i < roundTitleStrings.Length; i++)
		{
			if (roundTitleStrings[i].type == titleType)
			{
				return roundTitleStrings[i].localizedString;
			}
		}
		return null;
	}
}
