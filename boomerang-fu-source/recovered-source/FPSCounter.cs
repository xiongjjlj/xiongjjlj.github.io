using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
	[Header("// Sample Groups of Data ")]
	public bool GroupSampling = true;

	public int SampleSize = 20;

	[Header("// Config ")]
	public TextMeshProUGUI TargetText;

	public int UpdateTextEvery = 1;

	public int MaxTextLength = 5;

	public bool Smoothed = true;

	public bool ForceIntResult = true;

	[Header("// System FPS (updates once/sec)")]
	public bool UseSystemTick;

	[Header("// Color Config ")]
	public bool UseColors = true;

	public Color Good = Color.green;

	public Color Okay = Color.yellow;

	public Color Bad = Color.red;

	public int OkayBelow = 60;

	public int BadBelow = 30;

	protected float[] FpsSamples;

	protected int SampleIndex;

	protected int TextUpdateIndex;

	private float _fps;

	private int _sysLastSysTick;

	private int _sysLastFrameRate;

	private int _sysFrameRate;

	public float Framerate => _fps;

	protected virtual void Reset()
	{
		SampleSize = 20;
		UpdateTextEvery = 1;
		MaxTextLength = 5;
		Smoothed = true;
		UseColors = true;
		Good = Color.green;
		Okay = Color.yellow;
		Bad = Color.red;
		OkayBelow = 60;
		BadBelow = 30;
		UseSystemTick = false;
		ForceIntResult = true;
	}

	protected virtual void Start()
	{
		FpsSamples = new float[SampleSize];
		for (int i = 0; i < FpsSamples.Length; i++)
		{
			FpsSamples[i] = 0.001f;
		}
		if (!TargetText)
		{
			base.enabled = false;
		}
	}

	protected virtual void Update()
	{
		if (GroupSampling)
		{
			Group();
		}
		else
		{
			SingleFrame();
		}
		string text = _fps.ToString(CultureInfo.CurrentCulture);
		SampleIndex = ((SampleIndex < SampleSize - 1) ? (SampleIndex + 1) : 0);
		TextUpdateIndex = ((TextUpdateIndex <= UpdateTextEvery) ? (TextUpdateIndex + 1) : 0);
		if (TextUpdateIndex == UpdateTextEvery)
		{
			TargetText.text = text.Substring(0, (text.Length < 5) ? text.Length : 5);
		}
		if (UseColors)
		{
			if (_fps < (float)BadBelow)
			{
				TargetText.color = Bad;
			}
			else
			{
				TargetText.color = ((_fps < (float)OkayBelow) ? Okay : Good);
			}
		}
	}

	protected virtual void SingleFrame()
	{
		_fps = (UseSystemTick ? ((float)GetSystemFramerate()) : (Smoothed ? (1f / Time.smoothDeltaTime) : (1f / Time.deltaTime)));
		if (ForceIntResult)
		{
			_fps = (int)_fps;
		}
	}

	protected virtual void Group()
	{
		FpsSamples[SampleIndex] = (UseSystemTick ? ((float)GetSystemFramerate()) : (Smoothed ? (1f / Time.smoothDeltaTime) : (1f / Time.deltaTime)));
		_fps = 0f;
		bool flag = true;
		int num = 0;
		while (flag)
		{
			if (num == SampleSize - 1)
			{
				flag = false;
			}
			_fps += FpsSamples[num];
			num++;
		}
		_fps /= FpsSamples.Length;
		if (ForceIntResult)
		{
			_fps = (int)_fps;
		}
	}

	protected virtual int GetSystemFramerate()
	{
		if (Environment.TickCount - _sysLastSysTick >= 1000)
		{
			_sysLastFrameRate = _sysFrameRate;
			_sysFrameRate = 0;
			_sysLastSysTick = Environment.TickCount;
		}
		_sysFrameRate++;
		return _sysLastFrameRate;
	}
}
