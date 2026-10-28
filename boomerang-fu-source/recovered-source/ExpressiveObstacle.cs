using System.Collections;
using UnityEngine;

public class ExpressiveObstacle : InteractiveObstacle
{
	public Renderer faceplate;

	public FacialExpression currentFacialExpression = FacialExpression.idle;

	private float facialExpressionTimer;

	private const float winceThreshold = 2025f;

	public Animator anim;

	private void Update()
	{
		if (facialExpressionTimer > 0f)
		{
			facialExpressionTimer -= Time.deltaTime;
		}
		else
		{
			SetFacialExpression(FacialExpression.idle);
		}
	}

	public override void Interact(Vector2 force)
	{
		base.Interact(force);
		if (force.sqrMagnitude >= 2025f)
		{
			SetFacialExpression(FacialExpression.hurt, 0.33f);
		}
	}

	public void ReactToKill(FacialExpression expression)
	{
		StartCoroutine(ReactToKillAfterDelay(Random.Range(0.08f, 0.15f), expression));
	}

	private IEnumerator ReactToKillAfterDelay(float delay, FacialExpression expression)
	{
		yield return new WaitForSeconds(delay);
		SetFacialExpression(expression, (Singleton<GameManager>.Instance.OpponentsLeftStandingNow() < 2) ? 2f : 0.7f);
		if (anim != null)
		{
			anim.SetTrigger("React");
		}
	}

	public void SetFacialExpression(FacialExpression expression, float duration = 0.7f)
	{
		if (currentFacialExpression == FacialExpression.idle && expression == FacialExpression.idle)
		{
			if (Chance.PercentageTrue(0.07f))
			{
				SetFaceSheetOffset(FacialExpression.blinkcontent);
				facialExpressionTimer = Random.Range(0.05f, 0.1f);
				currentFacialExpression = FacialExpression.blink;
			}
			return;
		}
		currentFacialExpression = expression;
		if (expression == FacialExpression.idle)
		{
			expression = FacialExpression.content;
		}
		SetFaceSheetOffset(expression);
		facialExpressionTimer = Random.Range(duration, duration + 0.1f);
	}

	private void SetFaceSheetOffset(FacialExpression expression)
	{
		float num = 4f;
		float num2 = (float)expression % num;
		float num3 = (int)Mathf.Floor((float)expression / num);
		Vector2 value = new Vector2(num2 / num, (num - 1f) / num - num3 / num);
		faceplate.material.SetTextureOffset("_MainTex", value);
	}
}
