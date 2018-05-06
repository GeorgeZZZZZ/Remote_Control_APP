using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Volume_Triger : MonoBehaviour {

    public float Simple_Time = 0.06f;   // collect simple for how long time to calculate avarge value
    public float Holding_Time = 0.5f;   // hold triger for how long time after no audio input
    public int Triger_Value;    // triger if input audio louder than this value
    public float Current_Microphone_Input_Volume = 0;
    public bool IsTrigered = false;

    private float timer_0 = 0;
    private float timer_1 = 0;
    List<float> volume_data = new List<float>();

    AudioClip _clipRecord = new AudioClip();
    AudioClip _new = new AudioClip();

    // Use this for initialization
    void Start() {
        _clipRecord = Microphone.Start(Microphone.devices[0], true, 999, 44100);
    }

    public void trynew()
    {
        Debug.Log("new delay");
        _new = Microphone.Start(Microphone.devices[0], true, 10, 44100);
    }

    // Update is called once per frame
    void FixedUpdate() {
        //return;
        if (!Microphone.IsRecording(Microphone.devices[0]))
            Debug.Log("stop record!!!");
        CalculateAvargeInTime(GetCurrentInputVolume());

        bool _hold_triger = false;
        if (Current_Microphone_Input_Volume >= Triger_Value)
        {
            IsTrigered = _hold_triger = true;
            timer_1 = Holding_Time;
        }

        if (!_hold_triger && timer_1 >= 0)
        {
            timer_1 -= Time.deltaTime;
        }

        if (timer_1 < 0) IsTrigered = false;
    }
    private void CalculateAvargeInTime(float _volume)
    {
        bool _trigerInternal = false;
        if (timer_0 <= 0) _trigerInternal = true;
        else timer_0 -= Time.deltaTime;   // start countting by miuns real time

        if (_trigerInternal)
        {
            timer_0 = Simple_Time;
            int i = 0;
            float _result = 0;
            foreach (float _v in volume_data)
            {
                _result += _v;
                i++;
            }
            _result = (_result / i) * 100;  // calculate avage data and return in percentage
            if (_result >= 0)
                Current_Microphone_Input_Volume = _result;
            else
                Current_Microphone_Input_Volume = 0;

            volume_data.Clear();    // clear cache
        }
        else
        {
            volume_data.Add(_volume);   // put valume data in to cache for next triger cycle
        }
    }
    private float GetCurrentInputVolume()
    {
        float levelMax = 0;
        int _sampleWindow = 128;
        float[] waveData = new float[_sampleWindow];
        int micPosition = Microphone.GetPosition(null) - (_sampleWindow + 1); // null means the first microphone
        if (micPosition < 0) return 0;

        _clipRecord.GetData(waveData, micPosition);
        //MonoBehaviour.Destroy(_clipRecord);
        // Getting a peak on the last 128 samples
        for (int i = 0; i < _sampleWindow; i++)
        {
            float wavePeak = Mathf.Abs(waveData[i]);
            if (levelMax < wavePeak)
            {
                levelMax = wavePeak;
            }
        }
        return levelMax;
    }
}


