using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public static Timer Instance;

    [SerializeField]
    private TextMeshProUGUI TimerTxt;

    [SerializeField]
    private float TimerValue = 40;
    private float RemainingTime;
    private bool TimerOn;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Optionally start the timer
        // LaunchTimer();
    }

    void Update()
    {
        if (TimerOn)
        {
            RemainingTime -= Time.deltaTime;
            if (RemainingTime <= 0)
            {
                RemainingTime = 0;
                TimerOn = false;
            }
            UpdateTimerDisplay();
        }
    }

    private void UpdateTimerDisplay()
    {
        int min = Mathf.FloorToInt(RemainingTime / 60);
        int sec = Mathf.FloorToInt(RemainingTime % 60);
        TimerTxt.text = string.Format("{0:00}:{1:00}", min, sec);
    }

    public void LaunchTimer()
    {
        this.TimerOn = true;
        this.RemainingTime = TimerValue;
        UpdateTimerDisplay();
    }

    public void StopTimer()
    {
        this.TimerOn = false;
        UpdateTimerDisplay();
    }
}
