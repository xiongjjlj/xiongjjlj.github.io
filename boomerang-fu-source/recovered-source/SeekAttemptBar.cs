using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SeekAttemptBar : MonoBehaviour
{
	public Player player;

	public TextMeshProUGUI text;

	public Animator anim;

	public Image background;

	private Color backgroundColor;

	private Color flashColor = new Color(1f, 0.5f, 0.5f);

	public AnimationCurve flashCurve;

	private float flashTimer;

	private const float flashDur = 0.5f;

	private RectTransform parentRect;

	private void Start()
	{
		parentRect = base.transform.parent.GetComponent<RectTransform>();
	}

	public void Init(Player player)
	{
		this.player = player;
		background = GetComponent<Image>();
		GetComponent<RectTransform>().localScale = Vector3.one;
		HSBColor hSBColor = HSBColor.FromColor(player.character.Color);
		hSBColor.a = 0.9f;
		hSBColor.b = Mathf.Min(hSBColor.b, 0.7f);
		backgroundColor = hSBColor.ToColor();
		base.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if (player != null && player.seekAttemptsRemaining == 1)
		{
			flashTimer = (flashTimer + Time.deltaTime) % 0.5f;
			background.color = Color.Lerp(backgroundColor, flashColor, flashCurve.Evaluate(flashTimer / 0.5f));
		}
		else
		{
			background.color = backgroundColor;
		}
	}

	private void LateUpdate()
	{
		FollowCandidate();
	}

	private void FollowCandidate()
	{
		if (player != null)
		{
			Vector2 screenPoint = Singleton<Camera3D>.Instance.gameCamera.WorldToScreenPoint(player.transform.position);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, Singleton<Camera3D>.Instance.gameCamera, out var localPoint);
			base.transform.localPosition = localPoint + new Vector2(0f, 100f);
			text.text = player.seekAttemptsRemaining.ToString();
		}
	}

	public void ShowSeekAttemptBar()
	{
		base.gameObject.SetActive(value: true);
		FollowCandidate();
		anim.ResetTrigger("Hide");
		anim.SetTrigger("Show");
		anim.ResetTrigger("Pulse");
	}

	public void HideSeekAttemptBar()
	{
		anim.ResetTrigger("Show");
		anim.SetTrigger("Hide");
		anim.ResetTrigger("Pulse");
	}

	public void PulseSeekBar()
	{
		anim.ResetTrigger("Show");
		anim.ResetTrigger("Hide");
		anim.SetTrigger("Pulse");
	}
}
