using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using UnityEngine.UI;

[RequireComponent(typeof(TCPIPConnenction))]
public class Keywords_analysis : MonoBehaviour
{
    public Dropdown Comm_Method;

    public bool Enable_TCPIP_Communication;
    public bool TCPIP_module_ready;
    private TCPIPConnenction tcpConn;

    public bool Enable_Bluetooth_Communication;
    public bool Bluetooth_module_ready;
    public BTChat_Testting bluetoothComm;

    public bool New_words_flag_detector;
    public string Recognized_Speach_str;
    public delegate void _newRespone(string _message, bool? _immediately = null);
    public event _newRespone New_Response_Event;
    public event _newRespone State_Num_Change_Event;

    public int state = 0;

    public string[] keyConfirm = new string[] { "是", "是的", "确认", "確認", "Yes", "yes", "Confirm", "confirm" };
    public string[] KeyUnit = new string[] { "毫米", "mm", "MM" };
    public string[] KeyDeviceForTCP = new string[] { "机器人", "機器人" };
    public string[] KeyDeviceForBluetooth = new string[] { "智能机械手", "智能機械手" };
    public string[] commandxPosAxis = new string[] { "Forward", "forward", "向前", "前" };
    public string[] commandxNegAxis = new string[] { "Backward", "backward", "向后", "后" };
    public string[] commandyPosAxis = new string[] { "向左", "向佐", "左", "佐" };
    public string[] commandyNegAxis = new string[] { "向右", "向佑", "右", "佑" };
    public string[] commandzPosAxis = new string[] { "向上", "上" };
    public string[] commandzNegAxis = new string[] { "向下", "下" };
    public string[] commandVerb = new string[] { "移动", "动", "移動", "動" };
    public string[] commandGrabClose = new string[] { "抓紧", "抓緊" };
    public string[] commandGrabOpen = new string[] { "松开", "鬆開" };
    public string[] commandConnectTCPServer = new string[] { "连接机器人", "連接機器人", "連結機器人", "链接机器人" };
    public string[] commandDisconnectTCPServer = new string[] { "断开机器人", "斷開機器人" };
    public string[] commandConnectServoBoard = new string[] { "连接智能机械手", "連接智能機械手", "連結智能機械手", "链接智能机械手" };
    public string[] commandHandGesture_One = new string[] { "给我1", "给我一", "给我壹" };
    public string[] commandHandGesture_Two = new string[] { "给我2", "给我二", "给我贰", "给我两" };
    public string[] commandHandGesture_Thr = new string[] { "给我3", "给我三", "给我叁" };
    public string[] commandHandGesture_Fou = new string[] { "给我4", "给我四", "给我是" };
    public string[] commandHandGesture_Fiv = new string[] { "给我5", "给我五", "给我伍" };
    public string[] commandHandGesture_Six = new string[] { "给我6", "给我六", "给我陆" };
    public string[] commandHandGesture_ComeOn = new string[] { "勾引" };
    public string[] commandHandGesture_ThumbUp = new string[] { "超级棒" };
    public string[] commandHandGesture_ThumbDown = new string[] { "垃圾" };
    public string[] commandHandGesture_PointTo = new string[] { "就是你" };
    public string[] commandHandGesture_StartDemo = new string[] { "演示" };
    public string[] commandHandGesture_StopDemo = new string[] { "停止演示" };

    public string[] commandHandEnable = new string[] { "使能" };
    public string[] commandHandDisable = new string[] { "关闭使能" };

    public string[] commandCNCExecutionStart = new string[] { "启动" };
    public string[] commandCNCExecutionEnd = new string[] { "号程序" };

    string[] commandxAxis = new string[] { "x轴", "X轴" };
    string[] KeyDebug = new string[] { "调试", "", "Debug", "debug" };

    float timer_bluetooth;

    private FrostweepGames.Plugins.GoogleCloud.SpeechRecognition.GCSpeechRecognition _speechRecognition;
    public List<string> recognized_str = new List<string>();

    // debug
    float debugTimer = 2;

    // Use this for initialization
    void Start()
    {
        //  communication module initialize
        if (gameObject.GetComponent("TCPIPConnenction") != null)
        {
            tcpConn = gameObject.GetComponent<TCPIPConnenction>();
            tcpConn.New_Receive_Message_Event += Receive_New_Massage_From_TCPIP_Handler;
            tcpConn.Socket_Connected_Event += TcpConn_Socket_Connected_Event;
            tcpConn.Socket_Disconnected_Event += TcpConn_Socket_Disconnected_Event;
            tcpConn.Socket_Fail_Event += TcpConn_Socket_Fail_Event;
            TCPIP_module_ready = true;
        }
        if (bluetoothComm != null)
        {
            Bluetooth_module_ready = true;
        }

        // get speech recongnition script
        _speechRecognition = FrostweepGames.Plugins.GoogleCloud.SpeechRecognition.GCSpeechRecognition.Instance;
        // get event
        _speechRecognition.RecognitionSuccessEvent += newWordsRecongize;

        if (Comm_Method != null)
        {
            Comm_Method.ClearOptions();
            Comm_Method.options.Add(new Dropdown.OptionData("TCP/IP"));
            Comm_Method.options.Add(new Dropdown.OptionData("Bluetooth"));
            Comm_Method.onValueChanged.AddListener(Comm_Method_Changed);

            Comm_Method.value = 1;
            Comm_Method.value = 0; // set defualt as TCPIP
        }
    }

    private void TcpConn_Socket_Fail_Event(string _s)
    {
        Debug.Log("TcpConn_Socket_Fail_Event" + _s);
        Send_New_Response_To_Event("Failed to connect Robot controller." + _s, true);
    }

    private void TcpConn_Socket_Disconnected_Event()
    {
        Debug.Log("TcpConn_Socket_Disconnected_Event");
        Send_New_Response_To_Event("Disconnected from Robot controller.", true);
    }

    private void TcpConn_Socket_Connected_Event()
    {
        Debug.Log("TcpConn_Socket_Connected_Event");
        Send_New_Response_To_Event("Successfully connected to Robot controller.", true);
    }
    private void FixedUpdate()
    {
        KeywordsIdentify();
    }

    // Update is called once per frame
    void Update()
    {
        /* debug
        string findTheWord = "向下50毫米";
        /*
        string aa = "向下";
        Debug.Log("-------------------------------");
        Debug.Log(findTheWord.IndexOf("向下"));
        Debug.Log(findTheWord.IndexOf("毫米"));
        int len = findTheWord.IndexOf(aa) + aa.Length;
        Debug.Log(len);
        Debug.Log(findTheWord.Substring(len, findTheWord.IndexOf("毫米") - len));
        //*/
        /*
        string findTheWord = "智能机械手抓紧";
        /*
        foreach (string _keyword in commandGrabClose)
        {
            Debug.Log(_keyword + ": " + findTheWord.Contains(_keyword));
        }
        Debug.Log(simpleFilt(findTheWord, commandGrabClose));
        
        //*/
    }

    /******************
     * Functions
     ******************/

    private void Receive_New_Massage_From_TCPIP_Handler(TCPIPConnenction.Robot_State newState)
    {
        // robot hand gesture state
        if (newState.Gesture_State != null)
        {
            string _tag = null;

            if (newState.Gesture_State > 0 && newState.Gesture_State <= 6)
                _tag += "number " + newState.Gesture_State;

            switch (newState.Gesture_State)
            {
                case 10: _tag += "Fist"; break;
                case 11: _tag += "Palm"; break;
                case 12: _tag += "Thumb up"; break;
                case 13: _tag += "Thumb down"; break;
                case 14: _tag += "Come baby"; break;
                case 15: _tag += "Point to"; break;
                case 16: _tag += "Waving"; break;
                case 17: _tag += "Cat hand"; break;
            }

            if (newState.Gesture_State != 0)
                Send_New_Response_To_Event("Hand is performing " + _tag + ".");
        }

        // robot grab state
        if (newState.Grab_State == true)
            Send_New_Response_To_Event("Grab Closed.");
        else if (newState.Grab_State == false)
            Send_New_Response_To_Event("Grab Opened.");

        // hand enable state
        if (newState.Hand_Enabled == true)
                Send_New_Response_To_Event("Hand enabled.");
        else if (newState.Hand_Enabled == false)
            Send_New_Response_To_Event("Hand disabled.");

        // hand enable error
        if (newState.Hand_Enb_Err == true)
            Send_New_Response_To_Event("Hand enable error, hand did not enable.");
        
        // hand movement error
        if (newState.Hand_Mov_Err == true)
            Send_New_Response_To_Event("Hand movement error.");
        
        // robot movement
        if (newState.Robot_Mov_Err == true)
            Send_New_Response_To_Event("Robot movement error.");
    }

    void Send_New_Response_To_Event(string _str, bool? _when = null)
    {
        if (New_Response_Event != null) // if there are some where listening this event
            New_Response_Event(_str, _when);
    }

    void New_State_Num_Change(string _str)
    {
        if (State_Num_Change_Event != null) // if there are some where listening this event
            State_Num_Change_Event(_str);
    }

    private float commandNum;
    private float? commandNumNullable = null;
    private string tag;
    List<string> old_str = new List<string>(); // save new recognized sentences to this cache
    private void KeywordsIdentify()
    {
        int _oldState = state;
        // if there is a new recognized sentence come and current is not processing
        if (recognized_str.Count > 0 && old_str.Count == 0)
        {
            //  can not use old_str = recognized_str;
            //  only reference to to recognized_str and if recognized_str has been clear
            //  old_str will be clear too
            old_str.AddRange(recognized_str);
            recognized_str.Clear();
        }
        if (old_str.Count == 0) return;

        bool _TCP_not_live = false;
        bool _BlueTooth_not_live = false;
        // if TCP comm module has error or not connect yet
        if (tcpConn.Err_0_CommunicationError || !tcpConn.controlBoardSocketReadyFlag) _TCP_not_live = true;
        // if not connect a bluetooth device
        if (!bluetoothComm.Device_Connected) _BlueTooth_not_live = true;

        switch (state)
        {
            case 0:
                if (old_str.Count > 0)
                {
                    state = 30;
                }
                /*
                if (simpleFilt(lineFirst, keyName))
                {
                    Send_New_Response_To_Event = true;
                    Response_str = "Yes, Nana is watting for your command";
                    state = 10;
                    isNewCommand = false;
                }
                else if (simpleFilt(lineFirst, KeyDebug))
                {
                    Send_New_Response_To_Event = true;
                    Response_str = "Yes, watting for command";
                    state = 1000;
                }
                else state = 9999;

                isNewCommand = false;
                */
                //state = 30;
                break;
            case 20:
                if (Enable_TCPIP_Communication && TCPIP_module_ready)
                {
                    tcpConn.StartConnect(); // try to establish connection to tcp server
                }
                else Send_New_Response_To_Event("Did not select TCPIP communication method.", true);

                old_str.Clear();
                state = 0;
                break;
            case 21:
                if (Enable_Bluetooth_Communication && Bluetooth_module_ready)
                {
                    bluetoothComm.Button_Connect_to_Device_HC06();  // try to connect to bluetooth device
                    timer_bluetooth = 5;    // set timer
                    state = 22;
                }
                else
                {
                    Send_New_Response_To_Event("Did not select Bluetooth communication method.", true);
                    old_str.Clear();
                    state = 0;
                }

                break;
            case 22:
                timer_bluetooth -= Time.deltaTime;
                bool _stateDone = false;
                if (bluetoothComm.Device_Connected) // if connected
                {
                    _stateDone = true;
                    Send_New_Response_To_Event("Connected to Bluetooth servo board.", true);
                }
                else if (timer_bluetooth <= 0)  // if not connect and time reached
                {
                    _stateDone = true;
                    Send_New_Response_To_Event("Fail to connect Bluetooth servo board.", true);
                }
                if (_stateDone)
                {
                    old_str.Clear();
                    state = 0;
                }
                break;
            case 23:
                if (Enable_TCPIP_Communication && TCPIP_module_ready)
                {
                    tcpConn.StartDisconnect(); // disconnect from tcp server
                }
                else Send_New_Response_To_Event("Did not select TCPIP communication method.", true);

                old_str.Clear();
                state = 0;
                break;

            case 30:  // add new key word compare in this case
                /*
                 * State 20 for server connection
                 *  20 for tcp
                 *  21 for bluetooth
                 *  40 for robot movment
                 *  50 for hand command
                 */
                string _mached_keyword;
                string _midword;

                if (simpleFilt(old_str, commandConnectTCPServer, out _mached_keyword))
                {   // connect to TCP server
                    state = 20;
                }
                else if (simpleFilt(old_str, commandDisconnectTCPServer, out _mached_keyword))
                {   // disconnect from TCP server
                    state = 23;
                }
                else if (simpleFilt(old_str, commandConnectServoBoard, out _mached_keyword))
                {   // connect to Bluetooth servo control board
                    state = 21;
                }
                else if (secondFilt(old_str, commandxPosAxis, commandVerb, KeyUnit, out commandNumNullable))
                {   // move forward
                    tag = "X axis";
                    state = 40;
                }
                else if (secondFilt(old_str, commandxNegAxis, commandVerb, KeyUnit, out commandNumNullable))
                {   // move backward
                    commandNumNullable = -commandNumNullable;
                    tag = "X axis";
                    state = 40;
                }
                else if (secondFilt(old_str, commandyPosAxis, commandVerb, KeyUnit, out commandNumNullable))
                {   // move left
                    tag = "Y axis";
                    state = 40;
                }
                else if (secondFilt(old_str, commandyNegAxis, commandVerb, KeyUnit, out commandNumNullable))
                {   // move right
                    commandNumNullable = -commandNumNullable;
                    tag = "Y axis";
                    state = 40;
                }
                else if (secondFilt(old_str, commandzPosAxis, commandVerb, KeyUnit, out commandNumNullable))
                {   // move up
                    tag = "Z axis";
                    state = 40;
                }
                else if (secondFilt(old_str, commandzNegAxis, commandVerb, KeyUnit, out commandNumNullable))
                {   // move down
                    commandNumNullable = -commandNumNullable;
                    tag = "Z axis";
                    state = 40;
                }
                else if (simpleFilt(old_str, commandGrabClose, out _mached_keyword))
                {   // close grab
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Close Grab";
                }
                else if (simpleFilt(old_str, commandGrabOpen, out _mached_keyword))
                {   // open grab
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Open Grab";
                }
                else if (simpleFilt(old_str, commandHandGesture_One, out _mached_keyword))
                {   // Gesture one
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Give you one";
                }
                else if (simpleFilt(old_str, commandHandGesture_Two, out _mached_keyword))
                {   // Gesture two
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Give you two";
                }
                else if (simpleFilt(old_str, commandHandGesture_Thr, out _mached_keyword))
                {   // Gesture three
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Give you three";
                }
                else if (simpleFilt(old_str, commandHandGesture_Fou, out _mached_keyword))
                {   // Gesture four
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Give you four";
                }
                else if (simpleFilt(old_str, commandHandGesture_Fiv, out _mached_keyword))
                {   // Gesture five
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Give you five";
                }
                else if (simpleFilt(old_str, commandHandGesture_Six, out _mached_keyword))
                {   // Gesture six
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Give you six";
                }
                else if (simpleFilt(old_str, commandHandGesture_ComeOn, out _mached_keyword))
                {   // Gesture come on baby
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Give you come on";
                }
                else if (simpleFilt(old_str, commandHandGesture_ThumbUp, out _mached_keyword))
                {   // Gesture Thumb Up
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "thumb up";
                }
                else if (simpleFilt(old_str, commandHandGesture_ThumbDown, out _mached_keyword))
                {   // Gesture Thumb Down
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "thumb down";
                }
                else if (simpleFilt(old_str, commandHandGesture_PointTo, out _mached_keyword))
                {   // Gesture point to
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Point to";
                }
                else if (simpleFilt(old_str, commandHandGesture_StopDemo, out _mached_keyword))
                {   // stop show demo
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Stop Demo";
                }
                else if (simpleFilt(old_str, commandHandGesture_StartDemo, out _mached_keyword))
                {   // Start to show all Gesture as a demo, 
                    // this compare must lower than stop due to stop command
                    // contain same key word as start command, then it will recongize as start
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Show Demo";
                }
                else if (simpleFilt(old_str, commandHandDisable, out _mached_keyword))
                {   // Disable the hand
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Disable hand";
                }
                else if (simpleFilt(old_str, commandHandEnable, out _mached_keyword))
                {   // Enable the Hand
                    if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                    else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                    else state = 50;
                    tag = "Enable hand";
                }
                else if (SeparateFilt(old_str, commandCNCExecutionStart, commandCNCExecutionEnd, out _midword))
                {
                    float? _CNC_Num;
                    convertNum(_midword, out _CNC_Num);
                    if (_CNC_Num == null) state = 9999;
                    else state = 50;
                    tag = "CNC Move";
                    commandNumNullable = _CNC_Num;
                }
                else state = 9999;

                break;
            case 40:
                Send_New_Response_To_Event("ok, robot will move " + tag + " " + commandNumNullable + " mm, confirm?", true);
                state = 41;
                old_str.Clear();
                break;
            case 41:
                if (simpleFilt(old_str, keyConfirm, out _mached_keyword)) state = 60;
                else state = 9999;
                break;
            case 50:
                if (Comm_Method == null)
                {
                    Send_New_Response_To_Event("ok, robot will " + tag + ", which kind of device you want to control?", true);
                    state = 51;
                    old_str.Clear();
                }
                else
                {
                    if (Enable_TCPIP_Communication) state = 60; // if select TCP communication method
                    else state = 70;    // if select bluetooth
                }
                break;
            case 51:
                if (simpleFilt(old_str, KeyDeviceForTCP, out _mached_keyword)) state = 60;
                else if (simpleFilt(old_str, KeyDeviceForBluetooth, out _mached_keyword)) state = 70;
                else state = 9999;
                break;
            case 60:    // hand command send to tcp server
                if (_TCP_not_live)
                {
                    Send_New_Response_To_Event("Not yet connect to Robot controller.", true);
                }
                else
                {
                    string _movNum = null;
                    if (commandNumNullable != null) _movNum = " move " + commandNumNullable;

                    Send_New_Response_To_Event("Robot Command " + tag + _movNum + " has Sent.", true);

                    if (tag == "X axis")
                        tcpConn.SendCommandToControlBoard(_X: commandNumNullable);
                    else if (tag == "Y axis")
                        tcpConn.SendCommandToControlBoard(_Y: commandNumNullable);
                    else if (tag == "Z axis")
                        tcpConn.SendCommandToControlBoard(_Z: commandNumNullable);
                    else if (tag == "Close Grab")
                        tcpConn.SendCommandToControlBoard(_Grab: true);
                    else if (tag == "Open Grab")
                        tcpConn.SendCommandToControlBoard(_Grab: false);
                    else if (tag == "Give you one")
                        tcpConn.SendCommandToControlBoard(_Gesture: 1);
                    else if (tag == "Give you two")
                        tcpConn.SendCommandToControlBoard(_Gesture: 2);
                    else if (tag == "Give you three")
                        tcpConn.SendCommandToControlBoard(_Gesture: 3);
                    else if (tag == "Give you four")
                        tcpConn.SendCommandToControlBoard(_Gesture: 4);
                    else if (tag == "Give you five")
                        tcpConn.SendCommandToControlBoard(_Gesture: 5);
                    else if (tag == "Give you six")
                        tcpConn.SendCommandToControlBoard(_Gesture: 6);
                    else if (tag == "Give you come on")
                        tcpConn.SendCommandToControlBoard(_Gesture: 14);
                    else if (tag == "thumb up")
                        tcpConn.SendCommandToControlBoard(_Gesture: 12);
                    else if (tag == "thumb down")
                        tcpConn.SendCommandToControlBoard(_Gesture: 13);
                    else if (tag == "Point to")
                        tcpConn.SendCommandToControlBoard(_Gesture: 15);
                    else if (tag == "Show Demo")
                        tcpConn.SendCommandToControlBoard(_GestureDemo: true);
                    else if (tag == "Stop Demo")
                        tcpConn.SendCommandToControlBoard(_GestureDemo: false);
                    else if (tag == "Enable hand")
                        tcpConn.SendCommandToControlBoard(_HandEnable: true);
                    else if (tag == "Disable hand")
                        tcpConn.SendCommandToControlBoard(_HandEnable: false);
                    else if (tag == "CNC Move")
                        tcpConn.SendCommandToControlBoard(CNCMove: (int)commandNumNullable);
                }

                old_str.Clear();
                state = 0;
                break;
            case 70:    // hand command send to bluetooth server
                if (_BlueTooth_not_live)
                {
                    Send_New_Response_To_Event("Not yet connect to Bluetooth device.", true);
                    old_str.Clear();
                    state = 0;
                }
                else
                {
                    Send_New_Response_To_Event("Command sent to bluetooth device.", true);

                    for (int _motorNum = 0; _motorNum < 16; _motorNum++)
                        bluetoothComm.Send_Massage_To_Servo_Board(0x01, (byte)_motorNum, 0x0a, 0x00); // 0x01 9deg/s, 0x0a 90deg/s

                    state = 101;
                }
                break;
            case 101:
                if (tag == "Close Grab")
                {
                    for (int _motorNum = 0; _motorNum < 16; _motorNum++)
                        bluetoothComm.Send_Massage_To_Servo_Board(0x02, (byte)_motorNum, 0xf4, 0x01); // 0deg
                }
                else if (tag == "Open Grab")
                {
                    for (int _motorNum = 0; _motorNum < 16; _motorNum++)
                        bluetoothComm.Send_Massage_To_Servo_Board(0x02, (byte)_motorNum, 0xdc, 0x05);  // 90deg
                }
                old_str.Clear();
                state = 0;
                break;
            case 9999:
                Send_New_Response_To_Event("Cancel command or command not match.", true);
                old_str.Clear();
                state = 0;
                break;
        }
        // debug print state turnning message
        if (_oldState != state)
        {
            Debug.Log("IN state " + state);
            New_State_Num_Change("Turnning state: " + state.ToString());
        }
    }

    // simple filter to search for mach keyword
    private bool simpleFilt(List<string> sentences, string[] keyWords, out string matched_keyword)
    {
        bool _result = false;
        matched_keyword = null;
        foreach (string _keyword in keyWords)
        {
            foreach (var _str in sentences)
            {
                if (_str.Contains(_keyword))
                {
                    _result = true;
                    matched_keyword = _keyword;
                }
            }
        }
        return _result;
    }

    // second filter to search robot movement command and extruct value
    private bool secondFilt(List<string> sentence, string[] dir, string[] verb, string[] unit
        , out float? numOut)
    {
        bool _result = false;
        numOut = null;
        int _state = 0;
        int _numBegNum = -1;
        //string _str = sentence;
        int _mached_index = -1;

        while (_state < 100)
        {
            if (_state > 0 && _mached_index == -1) // provent index lost
            {
                Debug.Log("second filter internal error!!!");
                return false;
            }
            switch (_state)
            {
                case 0: // find movement direction keyword
                    foreach (string _d in dir)
                    {
                        int _num, _i = -1;
                        foreach (var _str in sentence)
                        {
                            _i++;  // start first index at 0
                            _num = _str.IndexOf(_d);    // maching keywords

                            if (_num > -1)  // if found
                            {
                                _mached_index = _i; // asign index of the str list
                                _numBegNum = _num + _d.Length;  // asign begin index of the matched keyword
                                _state = 10;
                                break;
                            }
                        }
                    }
                    if (_numBegNum < 0) _state = 999;
                    break;
                case 10:    // looing for verb
                    if (verb != null)
                    {
                        foreach (string _v in verb)
                        {
                            int _num = sentence[_mached_index].IndexOf(_v);    // maching keywords
                            if (_num > -1)  // found!!
                            {
                                if (_num + _v.Length > _numBegNum) // if verb is behand direction
                                    _numBegNum = _num + _v.Length;  // asign the begin index for number
                                break;
                            }
                        }
                    }
                    _state = 20; // no matter if match a verb, move to next state
                    break;
                case 20:    // looking for number
                    foreach (string _u in unit)
                    {
                        int _num_index = sentence[_mached_index].IndexOf(_u);    // maching keywords
                        if (_num_index > -1)  // if found
                        {
                            string _num_str;
                            try
                            {  // _num - _numBegNum take out number between unit and direction or verb
                                _num_str = sentence[_mached_index].Substring(_numBegNum, _num_index - _numBegNum);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                Debug.Log("Error trying to extract number from command in secondFilt(),\n" +
                                    " the error string is: " + sentence[_mached_index]);
                                return _result;
                            }

                            float? _ex;
                            convertNum(_num_str, out _ex);
                            if (_ex == null) return _result;
                            else numOut = _ex;

                            _result = true;
                            _state = 900;
                            break;
                        }
                        else _numBegNum = -1;
                    }
                    if (_numBegNum < 0) _state = 999;
                    break;
                case 900:
                    break;
                case 999:
                    break;
            }
        }
        return _result;
    }

    private void convertNum (string strIn, out float? numOut)
    {
        try
        {
            numOut = Convert.ToSingle(strIn);
        }
        catch (FormatException)
        {
            float? _ex = Convert_Chinese_Number_to_Arabic_Number(strIn);
            if (_ex == null)    // if is not in chinese number char
            {
                Debug.Log("Error trying to convert number from command in secondFilt(),\n" +
                    " the error data is: " + strIn);
            }

            numOut = _ex;
        }
    }
    // filter to search for two separate keywords and return value between two keywords
    private bool SeparateFilt(List<string> sentences, string[] FirstkeyWords, string[] SecondkeyWords, out string MeddleWords)
    {
        bool _result = false;
        MeddleWords = null;
        foreach (string _Fkey in FirstkeyWords) // loop first keywords
        {
            foreach (var _str in sentences) // loop the sentences
            {
                int _firIndex = _str.IndexOf(_Fkey); // start search first keyword from sentence
                if (_firIndex >= 0) // if find first keyword
                {
                    foreach (string _Skey in SecondkeyWords) // loop second keywords
                    {
                        int _secIndex = _str.IndexOf(_Skey);  // start search second keyword from sentence
                        if (_secIndex >= 0 && _secIndex > _firIndex)    // if find second and the order is later then first
                        {
                            int _startPos = (FirstkeyWords.Length + _firIndex + 1);
                            int _len = _secIndex - _startPos;
                            // get string from the end of first keyword to begainning of second keyword
                            MeddleWords = _str.Substring(_startPos,_len);
                            _result = true;
                        }
                    }
                }
            }
        }
        return _result;
    }
    private float? Convert_Chinese_Number_to_Arabic_Number(string _string)
    {
        float? _result = null;
        //byte[] _s = System.Text.Encoding.UTF8.GetBytes(_string);
        //int _len = _string.Length;
        foreach (char _s in _string)    // quite uselss to use foreach in this function
        {
            switch (_s)
            {
                case '零':
                    _result = 0;
                    break;
                case '一':
                case '壹':
                    _result = 1;
                    break;
                case '二':
                case '贰':
                case '两':
                    _result = 2;
                    break;
                case '三':
                case '叁':
                    _result = 3;
                    break;
                case '四':
                case '肆':
                    _result = 4;
                    break;
                case '五':
                case '伍':
                    _result = 5;
                    break;
                case '六':
                case '陆':
                    _result = 6;
                    break;
                case '七':
                case '柒':
                case '期':
                    _result = 7;
                    break;
                case '八':
                case '捌':
                case '吧':
                case '爸':
                    _result = 8;
                    break;
                case '九':
                case '玖':
                case '就':
                    _result = 9;
                    break;
            }
            //_len--;
        }
        return _result;
    }


    private int countNum = 0;
    private bool ComparerTwoStringArray(List<string> _first, string[] _second, int _startNum, out int countNum)
    {
        int _i = 0;
        bool _result = false;

        while (_i + _startNum < _first.Count)
        {
            string _f = _first[_i + _startNum];
            foreach (string _s in _second)
            {
                if (_f == _s)
                {
                    _result = true;
                    break;
                }
            }

            if (_result == true) break;
            _i++;
        }
        countNum = _i + 1;
        return _result;
    }

    private void Comm_Method_Changed(int _v)
    {
        switch (_v)
        {
            case 0:
                Enable_TCPIP_Communication = true;
                Enable_Bluetooth_Communication = false;
                break;
            case 1:
                Enable_TCPIP_Communication = false;
                Enable_Bluetooth_Communication = true;
                break;
        }
    }

    // debug
    void newWordsRecongize(FrostweepGames.Plugins.GoogleCloud.SpeechRecognition.RecognitionResponse _obj
                            , long requestIndex)
    {
        if (_obj == null || _obj.results.Length <= 0) return;    // if no words has been detected

        foreach (var _result in _obj.results)
        {
            foreach (var _alt in _result.alternatives)
            {
                recognized_str.Add(_alt.transcript);
            }
        }

        foreach (var _s in recognized_str)
            Debug.Log("newWordsRecongize:\n" + _s);
    }
}
