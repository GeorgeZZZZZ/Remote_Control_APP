using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AUP;
using System;

public class AndroidUltimatePlugin_Worker : MonoBehaviour
{
    public Keywords_analysis keywords_analysis;

    public Slider Speech_Pitch_Slider;
    public Slider Speech_Rate_Slider;
    public Slider Speech_Volume_Slider;
    private bool all_control_slider_is_assigned;

    public float Speech_Pitch = 1f;
    public float Speech_Rate = 1f;
    public int Speech_Volume = 10;

    private SpeechPlugin speechPlugin;
    private TextToSpeechPlugin textToSpeechPlugin;
    private float waitingInterval = 2f;

    private Dispatcher dispatcher;
    private UtilsPlugin utilsPlugin;

    private float timer = 3;

    private void Awake()
    {
#if UNITY_ANDROID
        if (Application.platform != RuntimePlatform.Android) return;

        dispatcher = Dispatcher.GetInstance();
        // for accessing audio
        utilsPlugin = UtilsPlugin.GetInstance();
        utilsPlugin.SetDebug(0);

        speechPlugin = SpeechPlugin.GetInstance();
        speechPlugin.SetDebug(0);

        textToSpeechPlugin = TextToSpeechPlugin.GetInstance();
        textToSpeechPlugin.SetDebug(0);
        textToSpeechPlugin.Initialize();
        textToSpeechPlugin.OnInit += OnInit;
        textToSpeechPlugin.OnChangeLocale += OnSetLocale;
        textToSpeechPlugin.OnStartSpeech += OnStartSpeech;
        textToSpeechPlugin.OnEndSpeech += OnEndSpeech;
        textToSpeechPlugin.OnErrorSpeech += OnErrorSpeech;
#endif
    }

    public void Stop()
    {
        if (textToSpeechPlugin != null)
        {
            textToSpeechPlugin.Stop();
        }
    }

    //checks if speaking
    public bool IsSpeaking()
    {
        return textToSpeechPlugin.IsSpeaking();
    }

    private void OnDestroy()
    {
        //call this of your not going to used TextToSpeech Service anymore
        textToSpeechPlugin.ShutDownTextToSpeechService();
    }

    private void OnDisable()
    {
        keywords_analysis.New_Response_Event -= New_Response_To_Speak;
        speech_queue.Clear();
        Stop_Talking();
    }
    private void OnEnable()
    {
        /*
         * onenable() is earlier than start() and has been call with start
         * at begain
         * but after disabled in runtime
         * only onenable will be call
         */
        if (keywords_analysis != null)
            keywords_analysis.New_Response_Event += New_Response_To_Speak;
    }

    private void OnInit(int status)
    {
        dispatcher.InvokeAction(
            () =>
            {
                Debug.Log("OnInit status: " + status);

                if (status == 1)
                {
                    //UpdateStatus("init speech service successful!");

                    //get available locale on android device
                    //textToSpeechPlugin.GetAvailableLocale();

                    //deleted!!!
                    //UpdateLocale(SpeechLocale.US);
                    //UpdatePitch(1f);
                    //UpdateSpeechRate(1f);

                    CancelInvoke("WaitingMode");
                    Invoke("WaitingMode", waitingInterval);
                }
                else
                {
                    //UpdateStatus("init speech service failed!");

                    CancelInvoke("WaitingMode");
                    Invoke("WaitingMode", waitingInterval);
                }
            }
        );
    }

    private void OnSetLocale(int status)
    {
        dispatcher.InvokeAction(
            () =>
            {
                Debug.Log("OnSetLocale status: " + status);
                if (status == 1)
                {
                    //float pitch = Random.Range(0.1f,2f);
                    //textToSpeechPlugin.SetPitch(pitch);
                }
            }
        );
    }

    private void OnStartSpeech(string utteranceId)
    {
        dispatcher.InvokeAction(
            () =>
            {
                //UpdateStatus("Start Speech...");
                Debug.Log("OnStartSpeech utteranceId: " + utteranceId);

                if (IsSpeaking())
                {
                    //UpdateStatus("speaking...");
                }
            }
        );
    }

    private void OnEndSpeech(string utteranceId)
    {
        dispatcher.InvokeAction(
            () =>
            {
                //UpdateStatus("Done Speech...");
                Debug.Log("OnDoneSpeech utteranceId: " + utteranceId);

                CancelInvoke("WaitingMode");
                Invoke("WaitingMode", waitingInterval);
            }
        );
    }

    private void OnErrorSpeech(string utteranceId)
    {
        dispatcher.InvokeAction(
            () =>
            {
                //UpdateStatus("Error Speech...");

                CancelInvoke("WaitingMode");
                Invoke("WaitingMode", waitingInterval);

                Debug.Log("OnErrorSpeech utteranceId: " + utteranceId);
            }
        );
    }
    
    void Start()
    {

#if UNITY_ANDROID
        if (Application.platform != RuntimePlatform.Android) return;

        textToSpeechPlugin.SetLocale(SpeechLocale.UK);

        if (Speech_Pitch_Slider != null
            && Speech_Rate_Slider != null
            && Speech_Volume_Slider != null)
        {
            all_control_slider_is_assigned = true;

            Speech_Pitch_Slider.maxValue = 2;
            Speech_Pitch_Slider.minValue = 0.1f;
            Speech_Pitch_Slider.value = Speech_Pitch;

            Speech_Rate_Slider.maxValue = 2;
            Speech_Rate_Slider.minValue = 0.1f;
            Speech_Rate_Slider.value = Speech_Rate;

            Speech_Volume_Slider.maxValue = 15;
            Speech_Volume_Slider.minValue = 0;
            Mathf.Clamp(Speech_Volume, 0, 15);
            Speech_Volume_Slider.value = Speech_Volume;
        }
        else
        {
            textToSpeechPlugin.SetPitch(Speech_Pitch);
            textToSpeechPlugin.SetSpeechRate(Speech_Rate);
            Mathf.Clamp(Speech_Volume, 0, 15);
            utilsPlugin.IncreaseMusicVolumeByValue(Speech_Volume); // 0 to 15, max 15
        }
#endif
    }

    void Update()
    {
        if (keywords_analysis == null)
        {
            if (timer <= 0)
            {
                timer = 3;
                Speech_Management("Respone source didn't sign in for TTS Module!!!", true);
            }
            else timer -= Time.deltaTime;
            return;
        }

        Speech_Management();
    }
    
    void New_Response_To_Speak(string _str, bool? _immediately)
    {
        Speech_Management(_str, _immediately);
    }

    private void Talk(string _sting)
    {
        string utteranceId = "test-utteranceId";

        if (textToSpeechPlugin.isInitialized())
        {
            // un mute volume
            utilsPlugin.UnMuteBeep();
            textToSpeechPlugin.SpeakOut(_sting, utteranceId);
        }
    }

    public void Stop_Talking()
    {
        if (textToSpeechPlugin != null)
        {
            textToSpeechPlugin.Stop();
        }
    }

    private List<string> speech_queue = new List<string>();
    private void Speech_Management(string newSpeech = null, bool? immediately = null)
    {
        // clear the queue if require immediately speek
        if (immediately == true) speech_queue.Clear();

        // only add new line if there are some thing to speek
        if (newSpeech != null) speech_queue.Add(newSpeech);
        
        if (speech_queue.Count == 0) return;    // if nothing to talk then return

#if UNITY_ANDROID
        if (Application.platform == RuntimePlatform.Android)
        {
            if (immediately == true) Talk(speech_queue[0]);
            else
            {
                // if is talking then return
                if (IsSpeaking()) return;
                Talk(speech_queue[0]);
            }
        }
#endif
        speech_queue.RemoveAt(0);   // remove the sentence after give talk assignment
    }

    /***********************************
     * button functions
     * *********************************/
    public void Button_Change_Speech_Pitch()
    {
        if (all_control_slider_is_assigned)
        {
            textToSpeechPlugin.SetPitch(Speech_Pitch_Slider.value);
        }
    }
    public void Button_Change_Speech_Rate()
    {
        if (all_control_slider_is_assigned)
        {
            textToSpeechPlugin.SetSpeechRate(Speech_Rate_Slider.value);
        }
    }
    public void Button_Change_Speech_Volume()
    {
        if (all_control_slider_is_assigned)
        {
            int _v = (int)Speech_Volume_Slider.value;
            Mathf.Clamp(_v, 0, 15);
            utilsPlugin.IncreaseMusicVolumeByValue(_v); // 0 to 15, max 15
        }
    }
}
