using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FrostweepGames.Plugins.GoogleCloud.SpeechRecognition
{
    public class FrostweepGame_GUI_3_0 : MonoBehaviour
    {
        public Keywords_analysis keywords_analysis;
        public AndroidUltimatePlugin_Worker AUP_Worker;

        private Button recordButton, runtimeButton;

        private GCSpeechRecognition _speechRecognition;

        private Image _speechRecognitionState;

        public Text _speechRecognitionResult;

        private Dropdown _languageDropdown;

        private bool run_time_detect_flag;

        public int text_clean_length = 200;

        private void Start()
        {
            _speechRecognition = GCSpeechRecognition.Instance;
            _speechRecognition.RecognitionSuccessEvent += SpeechRecognizedSuccessEventHandler;
            _speechRecognition.RecognitionFailedEvent += SpeechRecognizedFailedEventHandler;

            recordButton = transform.Find("Canvas_Main/Button_StartRecord").GetComponent<Button>();
            runtimeButton = transform.Find("Canvas_Main/Button_AutoDetect").GetComponent<Button>();

            _speechRecognitionState = transform.Find("Canvas_Main/Image_RecordState").GetComponent<Image>();

            _speechRecognitionResult = transform.Find("Canvas_Main/Text_Result").GetComponent<Text>();

            _languageDropdown = transform.Find("Canvas_Setting/Dropdown_Language").GetComponent<Dropdown>();

            _speechRecognitionState.color = Color.white;

            _languageDropdown.ClearOptions();

            for (int i = 0; i < Enum.GetNames(typeof(Enumerators.LanguageCode)).Length; i++)
            {
                _languageDropdown.options.Add(new Dropdown.OptionData(((Enumerators.LanguageCode)i).ToString()));
            }

            _languageDropdown.onValueChanged.AddListener(LanguageDropdownOnValueChanged);

            _languageDropdown.value = 40; // simple chinese, not woring currenttly

            if (keywords_analysis == null)
            {
                _speechRecognitionResult.text = "\nSpeech key words analysis module not assign, can not recongise any command!!\n";
            }
            else
            {
                keywords_analysis.New_Response_Event += Response_Handle;
                keywords_analysis.State_Num_Change_Event += State_Num_Change_Handle;
            }


            if (AUP_Worker == null)
                _speechRecognitionResult.text += "\nTTS module not assign!!\n";
        }

        private void OnDestroy()
        {
            _speechRecognition.RecognitionSuccessEvent -= SpeechRecognizedSuccessEventHandler;
            _speechRecognition.RecognitionFailedEvent -= SpeechRecognizedFailedEventHandler;
        }


        private void StartRecordButtonOnClickHandler()
        {
            _speechRecognitionState.color = Color.red;
            _speechRecognitionResult.text = string.Empty;
            _speechRecognition.StartRecord(run_time_detect_flag);
        }

        private void StopRecordButtonOnClickHandler()
        {
            _speechRecognitionState.color = Color.yellow;
            _speechRecognition.StopRecord();
        }

        private void LanguageDropdownOnValueChanged(int value)
        {
            _speechRecognition.SetLanguage((Enumerators.LanguageCode)value);
        }

        private void SpeechRecognizedFailedEventHandler(string obj, long requestIndex)
        {
            _speechRecognitionResult.text = "Speech Recognition failed with error: " + obj;

            if (!run_time_detect_flag)
            {
                _speechRecognitionState.color = Color.green;
            }
        }

        private void SpeechRecognizedSuccessEventHandler(RecognitionResponse obj, long requestIndex)
        {
            if (!run_time_detect_flag)
            {
                _speechRecognitionState.color = Color.green;
            }

            if (obj == null || obj.results.Length < 0)
            {
                _speechRecognitionResult.text = "Speech Recognition succeeded but no words detected.";
            }
            else
            {
                _speechRecognitionResult.text = "Possible words:";
                foreach (var _result in obj.results)
                {
                    foreach (var _alt in _result.alternatives)
                    {
                        _speechRecognitionResult.text += "\n" + _alt.transcript;
                    }
                }
                
            }
        }

        private void Response_Handle(string _str, bool? _immedialy)
        {
            // clear the screen if there are too many things
            if (_speechRecognitionResult.text.Length > text_clean_length)
                _speechRecognitionResult.text = string.Empty;

            _speechRecognitionResult.text += "\nResponse:\n" + _str;
        }

        private void State_Num_Change_Handle(string _str, bool? _immedialy)
        {
            _speechRecognitionResult.text += "\n" + _str;
        }

        /***************************
         * button functions
         * *************************/
        public void Button_Push_And_Record()
        {
            AUP_Worker.enabled = false;
            runtimeButton.interactable = false;
            StartRecordButtonOnClickHandler();
        }
        public void Button_Release_And_Stop()
        {
            AUP_Worker.enabled = true;
            runtimeButton.interactable = true;
            StopRecordButtonOnClickHandler();
        }
        public void Button_Runtime_Detection()
        {
            if (!run_time_detect_flag)
            {
                recordButton.interactable = false;
                run_time_detect_flag = true;
                StartRecordButtonOnClickHandler();
            }
            else
            {
                recordButton.interactable = true;
                run_time_detect_flag = false;
                StopRecordButtonOnClickHandler();
            }
        }
    }
}