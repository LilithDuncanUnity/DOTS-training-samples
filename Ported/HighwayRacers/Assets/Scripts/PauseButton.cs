using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseButton : MonoBehaviour
{
	public Text text;

	public void onButtonPress()
	{
		if (Time.timeScale > 0)
		{
			prevTimeScale = Time.timeScale;
			Time.timeScale = 0;
			text.text = "Resume";
		}
		else
		{
			Time.timeScale = prevTimeScale;
			text.text = "Pause";
		}
	}

	private float prevTimeScale = 1;
}
