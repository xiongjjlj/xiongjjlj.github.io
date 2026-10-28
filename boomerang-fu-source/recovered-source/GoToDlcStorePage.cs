using System;
using TKOU_DLC_System;
using UnityEngine;

public class GoToDlcStorePage : MonoBehaviour
{
	public void OpenDlcStorePage()
	{
		string text = Enum.GetName(typeof(DownloadableContentID), DownloadableContentID.FreshFlavorsPack);
		if (text != null)
		{
			DlcPlatformManager.Instance.OpenInStore(text);
		}
	}

	public void OpenDetailsPage(DownloadableContentID dlcId)
	{
		string text = Enum.GetName(typeof(DownloadableContentID), dlcId);
		if (text != null)
		{
			DlcPlatformManager.Instance.OpenInStore(text);
		}
	}
}
