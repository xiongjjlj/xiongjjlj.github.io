using UnityEngine;

public class MovingObstacleButter : MovingObstacle
{
	public Animator butterAnimator;

	protected override void OnReachedTarget()
	{
		base.OnReachedTarget();
		if (posIndex == 0)
		{
			butterAnimator.ResetTrigger("SquishBottom");
			butterAnimator.SetTrigger("SquishBottom");
		}
		else
		{
			butterAnimator.ResetTrigger("SquishTop");
			butterAnimator.SetTrigger("SquishTop");
		}
	}
}
