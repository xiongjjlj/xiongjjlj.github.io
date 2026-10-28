using UnityEngine;

public class PowerupCounter : MonoBehaviour
{
	public GameObject targetPlayer;

	private Player targetPlayerCandidate;

	public int targetNumberOfDiscs;

	private GameObject highlight;

	private GameObject count;

	private void Start()
	{
		targetPlayerCandidate = targetPlayer.GetComponent<Player>();
		highlight = base.transform.Find("Highlight").gameObject;
		count = base.transform.Find("Count").gameObject;
	}

	private void Update()
	{
	}
}
