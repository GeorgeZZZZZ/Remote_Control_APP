using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Net.Sockets;
using System.IO;
using System;
using System.Threading;
using System.Net;
using System.Text;
using UnityEngine.UI;

public class Connect_and_Control : MonoBehaviour {

    public Slider Speed_Slider;
    public Slider Camera_Position_Vertical;
    public Text SpeedNum;
    public Toggle Light_First;
    public Toggle Light_Second;

    public int Global_Output_FixpoitNum = 2;
    public float ServoMotorPos_0 = 0;
    public float ServoMotoerFactor_0 = 0.5f;
    public float SpeedChangeFactor = 0.25f;
    public bool Light_0 = false;
    public bool Light_1 = false;
    public bool Debug_Flag_0 = false;
    public bool Reset_Mode = false;
    public bool Reset_Left = false;
    public bool Reset_Right = false;
    public bool Reset_Middle = false;

    public bool Err_0_CommunicationError = false;

    private bool Light_0_Compare = false;
    private bool Light_1_Compare = false;

    private float smallestNum;

    private bool controlBoardSocketReadyFlag;
    private TcpClient controlBoardTcp;
    private NetworkStream controlBoardStream;
    private List<byte> receiveList = new List<byte> ();

    private bool moveForward;
    private bool moveBackward;
    private bool moveLeft;
    private bool moveRight;
    private bool moveUp;
    private bool moveDown;
    private bool turnLeft;
    private bool turnRight;
    private bool camUp;
    private bool camDown;

    private float leftMotorSpeed = 0f;
    private float rightMotorSpeed = 0f;
    private float midMotorSpeed = 0f;

    private string controlBoardHost = "10.0.0.50";
    //private string controlBoardHost = "10.0.0.101";
    private int controlBoardPort = 23;

    private float timer_0 = 0f; // a timer control how long time send a command

    private bool joystickPlugIn;

    private bool joystick_button_a;
    private bool joystick_button_b;
    private bool joystick_button_x;
    private bool joystick_button_y;
    private bool joystick_button_start;
    private bool joystick_button_back;

    private void Start()
    {
        //Thread nThread = new Thread(new ThreadStart(ConnectedToControlBoard));
        //nThread.Start();
        //ConnectedToControlBoard();

        // calculate smallest number
        float _snDenominator = 1f;
        for (int _i = 0; _i < Global_Output_FixpoitNum; _i++)
        {
            _snDenominator *= 10;
        }
        if (_snDenominator == 1) smallestNum = 0;
        else smallestNum = 1 / _snDenominator;

        SpeedNum.text = "";
    }
    
    private void FixedUpdate()
    {
        bool _isConnected = false;
        if (controlBoardSocketReadyFlag)
        {
            Err_0_CommunicationError = false;
            _isConnected = IsConnected(); //  connection health check
        }

        if (!_isConnected && controlBoardSocketReadyFlag)
        {
            Err_0_CommunicationError = true;
            Debug.Log("Error: Control Board Disconnect");
            controlBoardSocketReadyFlag = false;
            controlBoardStream.Close();
            controlBoardTcp.Close();
        }

        if (controlBoardSocketReadyFlag)
        {
            if (controlBoardStream.DataAvailable)
            {
                byte[] _incomeBuffer = new byte[100];   //  tcpC.ReceiveBufferSize is 65536, I don' need a buffer that big
                controlBoardStream.Read(_incomeBuffer, 0, _incomeBuffer.Length);
                OnIncomingData(_incomeBuffer);
            }
        }

        InputManagement();

        SendMotorCommand();

        SendOtherCommand();
        
        DebugFuncton();

    }

    private void LateUpdate()
    {
        UIManagement();
    }

    /******************
     * Functions
     ******************/

    private void UIManagement ()
    {
        // assign speed value to UI
        byte[] _globalspeed = FixpointProcesser(Encoding.ASCII.GetBytes(Speed_Slider.value.ToString()));
        SpeedNum.text = Encoding.Default.GetString(_globalspeed) + "%";

        // assign cam pos to UI
        Camera_Position_Vertical.value = ServoMotorPos_0;

        // assign light states
        Light_First.isOn = Light_0;
        Light_Second.isOn = Light_1;
    }

    private void SendOtherCommand()
    {
        if (Light_0 != Light_0_Compare)
        {
            Light_0_Compare = Light_0;
            SendCommandToControlBoard(_light_0: Light_0_Compare = Light_0);
        }

        if (Light_1 != Light_1_Compare)
        {
            Light_1_Compare = Light_1;
            SendCommandToControlBoard(_light_1: Light_1);
        }
    }

    private void InputManagement ()
    {
        float ui_lmc, ui_rmc, ui_mmc, j_lmc, j_rmc, j_mmc;
        ui_lmc = ui_rmc = ui_mmc = j_lmc = j_rmc = j_mmc =
            0;

        UIButtonControl(out ui_lmc, out ui_rmc, out ui_mmc); //  UI button command

        JoystickControl(out j_lmc, out j_rmc, out j_mmc); // joystick control

        leftMotorSpeed = ui_lmc + j_lmc;
        rightMotorSpeed = ui_rmc + j_rmc;
        midMotorSpeed = ui_mmc + j_mmc;
    }

    public void DebugButton ()
    {
        Debug_Flag_0 = !Debug_Flag_0;
        Debug.Log("DebugButton!!!");
    }

    private void DebugFuncton ()
    {
        if (Debug_Flag_0) SendCommandToControlBoard(_GP: Speed_Slider.value, _DB: 100);
    }

    private void SendMotorCommand ()
    {
        float globalSpeed = Speed_Slider.value;


        if (timer_0 > 0) timer_0 -= Time.deltaTime;

        ServoMotorPos_0 = Mathf.Clamp(ServoMotorPos_0, -100, 100);

        if (timer_0 <= 0f)
        {
            timer_0 = 0.04f;   // send command every 0.04s


            float _lmc = Mathf.Clamp(leftMotorSpeed, -100, 100);    // limit value in between -100 and 100
            float _rmc = Mathf.Clamp(rightMotorSpeed, -100, 100);
            float _mmc = Mathf.Clamp(midMotorSpeed, -100, 100);

            //globalSpeed = Mathf.Clamp(globalSpeed, 0, 100);

            float? _giveLmc = null;
            float? _giveRmc = null;
            float? _giveMmc = null;

            if (_lmc != 0) _giveLmc = _lmc;
            if (_rmc != 0) _giveRmc = _rmc;
            if (_mmc != 0) _giveMmc = _mmc;

            if (controlBoardSocketReadyFlag)    // send command if control board connected
                SendCommandToControlBoard(_giveLmc, _giveRmc, _giveMmc, globalSpeed
                    , _servo_0: ServoMotorPos_0);   // send command
        }
    }

    void JoystickControl (out float _lmc, out float _rmc, out float _mmc)
    {
        _lmc = _rmc = _mmc = 0;
        byte joystickType = GetJoystickType();    // get joystick type
        if (joystickType == 0) return;    // no joystick
        
        float rollL, pitchL;
        float rollR, pitchR;
        float rollPadPositive, pitchPadPositive;
        float triggerPositive;
        bool button_a, button_b, button_x, button_y, button_back, button_start, button_left, button_right;

        rollL = pitchL = rollR = pitchR
            = rollPadPositive = pitchPadPositive
            = triggerPositive
            = 0;
        button_a = button_b = button_x = button_y = button_back = button_start = button_left = button_right
            = false;

        // get joysitck input
        if (joystickType == 1)  // if is NEWGAME 360 mode
        {
            GetJoystickInput_Windows360(out rollL, out pitchL, out rollR, out pitchR
                , out rollPadPositive, out pitchPadPositive
                , out triggerPositive
                , out button_a, out button_b, out button_x, out button_y, out button_back, out button_start, out button_left, out button_right);
        }
        
        _lmc = (pitchL * -100 + rollL * 100);   // return motor speed after calculation
        _rmc = (pitchL * -100 + rollL * -100);
        _mmc = triggerPositive * 100;

        // move servo motor for cam
        if (pitchPadPositive != 0)
            ServoMotorPos_0 += pitchPadPositive * ServoMotoerFactor_0;

        // change golbal speed
        if (rollPadPositive != 0)
            Speed_Slider.value += rollPadPositive * SpeedChangeFactor;

        // control light
        if (button_x) joystick_button_x = true;
        if (!button_x && joystick_button_x)
        {
            Light_0 = !Light_0;
            joystick_button_x = false;
        }

        if (button_y) joystick_button_y = true;
        if (!button_y & joystick_button_y)
        {
            Light_1 = !Light_1;
            joystick_button_y = false;
        }

        // select fast speed
        if (button_b) joystick_button_b = true;
        if (!button_b && joystick_button_b)
        {
            FastSpeedSelection();
            joystick_button_b = false;
        }

        // start connect cam and control board
        if (button_start) joystick_button_start = true;
        if (!button_start && joystick_button_start)
        {
            StartConnect();
            joystick_button_start = false;
        }

        // debugging
        if (true) return;

        if (rollL != 0)
        {
            Debug.Log("Horizontal Left: " + rollL);
        }

        if (pitchL != 0)
        {
            Debug.Log("Vertical Left: " + pitchL);
        }
        
        if (rollR != 0)
        {
            Debug.Log("Horizontal Right: " + rollR);
        }

        if (pitchR != 0)
        {
            Debug.Log("Vertical Right: " + pitchR);
        }

        if (rollPadPositive != 0)
        {
            Debug.Log("Horizontal Pad P: " + rollPadPositive);
        }

        if (pitchPadPositive != 0)
        {
            Debug.Log("Vertical Pad P: " + pitchPadPositive);
        }

        if (triggerPositive != 0)
        {
            Debug.Log("Trigger: " + triggerPositive);
        }

        if (button_a) Debug.Log("button_A True!!!");
        if (button_b) Debug.Log("button_b True!!!");
        if (button_x) Debug.Log("button_x True!!!");
        if (button_y) Debug.Log("button_y True!!!");
        if (button_back) Debug.Log("button_back True!!!");
        if (button_start) Debug.Log("button_start True!!!");
        if (button_left) Debug.Log("button_left True!!!");
        if (button_right) Debug.Log("button_right True!!!");
    }

    int fastSpeedState = 5;
    private void FastSpeedSelection()
    {
        switch (fastSpeedState)
        {
            case 5:
                Speed_Slider.value = fastSpeedState;
                fastSpeedState = 10;
                break;
            case 10:
                Speed_Slider.value = fastSpeedState;
                fastSpeedState = 50;
                break;
            case 50:
                Speed_Slider.value = fastSpeedState;
                fastSpeedState = 90;
                break;
            case 90:
                Speed_Slider.value = fastSpeedState;
                fastSpeedState = 5;
                break;
        }
    }

    byte GetJoystickType ()
    {
        byte _result = 0;   // a local value reset evey time to detect joystrick connection
        string [] _joystickName = Input.GetJoystickNames(); // put joystick name from all avaliable joystick slots

        foreach (string _name in _joystickName) // search slots
        {
            if (_name.Length > 0)   // if there is joystick connected in
            {
                if (_name == "Controller (Controller(XBOX 360 For Windows)")
                    _result = 1;    // NEWGAME 360 mode

                if (!joystickPlugIn)    // if first time a joystick plug in
                {
                    joystickPlugIn = true;
                    Debug.Log("Debug: " + _name);   // currently just show the name in debug, can show the name on screen later
                }
            }
        }

        if (_result == 0) joystickPlugIn = false;

        return _result;
    }

    void UIButtonControl (out float _lmc, out float _rmc, out float _mmc)
    {
        _lmc = _rmc = _mmc = 0;

        if (moveForward) _lmc = _rmc = 100;
        else if (moveBackward) _lmc = _rmc = -100;
        else if (moveUp) _mmc = 100;
        else if (moveDown) _mmc = -100;
        else if (turnLeft)
        {
            _lmc = 100; // test left motor
            //_rmc = 100;
        }
        else if (turnRight)
        {
            //_lmc = 100;
            _rmc = 100; // test right motor
        }

        if (camUp) ServoMotorPos_0 += ServoMotoerFactor_0;
        if (camDown) ServoMotorPos_0 -= ServoMotoerFactor_0;
    }
    
    // get and return all button and axis from a windowns 360 joystick
    private void GetJoystickInput_Windows360(out float _xAL, out float _yAL, out float _xAR, out float _yAR
        , out float _xAPP, out float _yAPP
        , out float _tAP
        , out bool _a, out bool _b, out bool _x, out bool _y, out bool _back, out bool _start, out bool _bl, out bool _br)
    {
        _xAL = _yAL = _xAR = _yAR
            = _xAPP = _yAPP
            = _tAP
            = 0f;

        float _xALReal = Input.GetAxis("Joystick_Horizontal_Left");
        float _yALReal = Input.GetAxis("Joystick_Vertical_Left");
        float _xARReal = Input.GetAxis("Joystick_Horizontal_Right");
        float _yARReal = Input.GetAxis("Joystick_Vertical_Right");
        float _xAPPReal = Input.GetAxis("Joystick_Horizontal_Pad_Positive");
        float _yAPPReal = Input.GetAxis("Joystick_Vertical_Pad_Positive");
        float _tAPReal = Input.GetAxis("Joystick_Trigger_Positive");

        _a = Input.GetButton("Button_A");
        _b = Input.GetButton("Button_B");
        _x = Input.GetButton("Button_X");
        _y = Input.GetButton("Button_Y");
        _back = Input.GetButton("Button_Back");
        _start = Input.GetButton("Button_Start");
        _bl = Input.GetButton("Button_Left");
        _br = Input.GetButton("Button_Right");


        // because joystick axis never return 0, so only give value when input larger than smallest number
        if (_xALReal <= -smallestNum || _xALReal >= smallestNum)    _xAL = _xALReal;

        if (_yALReal <= -smallestNum || _yALReal >= smallestNum)    _yAL = _yALReal;

        if (_xARReal <= -smallestNum || _xARReal >= smallestNum)    _xAR = _xARReal;

        if (_yARReal <= -smallestNum || _yARReal >= smallestNum)    _yAR = _yARReal;

        if (_xAPPReal <= -smallestNum || _xAPPReal >= smallestNum)  _xAPP = _xAPPReal;

        if (_yAPPReal <= -smallestNum || _yAPPReal >= smallestNum)  _yAPP = _yAPPReal;

        if (_tAPReal <= -smallestNum || _tAPReal >= smallestNum)    _tAP = _tAPReal;
    }

    //_L: left motor, _R: right motor, _M: middle motor , _GP: golbal speed, _DB: debug
    private void SendCommandToControlBoard(float? _L = null, float? _R = null, float? _M = null, float? _GP = null
        , float? _servo_0 = null
        , bool? _light_0 = null , bool? _light_1 = null
        , float? _DB = null
        , bool? _resetMode = null, bool? _resetL = null, bool? _resetR = null, bool? _resetM = null)
    {
        if (!controlBoardSocketReadyFlag)
        {
            Debug.Log("Error: Not connect to controlboard yet.");
            return;
        }

        List<byte> _newMessage = new List<byte>();    //  initinalize a list
        
        if (_L != null)
        {
            byte[] _leftMotor = FixpointProcesser (Encoding.ASCII.GetBytes (_L.ToString ())); //  transfer to ASCII

            _newMessage.Add(40);    // (
            _newMessage.Add(76);    // L
            _newMessage.Add(41);    // )
            _newMessage.AddRange(_leftMotor); // left motor speed
            _newMessage.Add(59);    // ;
        }
        if (_R != null)
        {
            byte[] _rightMotor = FixpointProcesser (Encoding.ASCII.GetBytes (_R.ToString ()));

            _newMessage.Add(40);    // (
            _newMessage.Add(82);    // R
            _newMessage.Add(41);    // )
            _newMessage.AddRange(_rightMotor); // right motor speed
            _newMessage.Add(59);    // ;
        }
        if (_M != null)
        {
            byte[] _midMotor = FixpointProcesser (Encoding.ASCII.GetBytes (_M.ToString ()));

            _newMessage.Add(40);    // (
            _newMessage.Add(77);    // M
            _newMessage.Add(41);    // )
            _newMessage.AddRange(_midMotor); // mid motor speed
            _newMessage.Add(59);    // ;
        }

        if (_GP != null)
        {
            byte[] _globalSpeed = FixpointProcesser(Encoding.ASCII.GetBytes(_GP.ToString()));

            _newMessage.Add(40);    // (
            _newMessage.Add(83);    // S
            _newMessage.Add(80);    // P
            _newMessage.Add(41);    // )
            _newMessage.AddRange(_globalSpeed); // global speed
            _newMessage.Add(59);    // ;
        }

        if (_servo_0 != null)
        {
            byte[] _servoPos = FixpointProcesser(Encoding.ASCII.GetBytes(_servo_0.ToString()));

            _newMessage.Add(40);    // (
            _newMessage.Add(83);    // S
            _newMessage.Add(69);    // E
            _newMessage.Add(41);    // )
            _newMessage.AddRange(_servoPos);    // servo position
            _newMessage.Add(59);    // ;
        }

        if (_light_0 != null)
        {
            byte _lightContril;

            if (_light_0 == true) _lightContril = 49; // one: true
            else _lightContril = 48;    // zero: false

            _newMessage.Add(40);    // (
            _newMessage.Add(76);    // L
            _newMessage.Add(48);    // 0
            _newMessage.Add(41);    // )
            _newMessage.Add(_lightContril);
            _newMessage.Add(59);    // ;
        }

        if (_light_1 != null)
        {
            byte _lightContril;

            if (_light_1 == true) _lightContril = 49; // one: true
            else _lightContril = 48;    // zero: false

            _newMessage.Add(40);    // (
            _newMessage.Add(76);    // L
            _newMessage.Add(49);    // 1
            _newMessage.Add(41);    // )
            _newMessage.Add(_lightContril);
            _newMessage.Add(59);    // ;
        }

        if (_DB != null)
        {
            byte[] _debug = FixpointProcesser(Encoding.ASCII.GetBytes(_DB.ToString()));

            _newMessage.Add(40);    // (
            _newMessage.Add(68);    // D
            _newMessage.Add(66);    // B
            _newMessage.Add(41);    // )
            _newMessage.AddRange(_debug); // debug speed
            _newMessage.Add(59);    // ;
        }

        if (_resetMode != null)
        {
            byte _rmode;

            if (_resetMode == true) _rmode = 49; // one: true
            else _rmode = 48;    // zero: false

            _newMessage.Add(40);    // (
            _newMessage.Add(82);    // R
            _newMessage.Add(69);    // E
            _newMessage.Add(41);    // )
            _newMessage.Add(_rmode);    // start thruster reset mode
            _newMessage.Add(59);    // ;
        }
        if (_resetL != null)
        {
            byte _rl;

            if (_resetL == true) _rl = 49; // one: true
            else _rl = 48;    // zero: false

            _newMessage.Add(40);    // (
            _newMessage.Add(82);    // R
            _newMessage.Add(108);   // l
            _newMessage.Add(41);    // )
            _newMessage.Add(_rl);   // reset left
            _newMessage.Add(59);    // ;
        }
        if (_resetR != null)
        {
            byte _rr;

            if (_resetR == true) _rr = 49; // one: true
            else _rr = 48;    // zero: false

            _newMessage.Add(40);    // (
            _newMessage.Add(82);    // R
            _newMessage.Add(114);   // r
            _newMessage.Add(41);    // )
            _newMessage.Add(_rr);   // reset right
            _newMessage.Add(59);    // ;
        }
        if (_resetM != null)
        {
            byte _rm;

            if (_resetM == true) _rm = 49; // one: true
            else _rm = 48;    // zero: false

            _newMessage.Add(40);    // (
            _newMessage.Add(82);    // R
            _newMessage.Add(109);   // m
            _newMessage.Add(41);    // )
            _newMessage.Add(_rm);   // reset middle
            _newMessage.Add(59);    // ;
        }

        _newMessage.Add(91); // [
        _newMessage.Add(13); // add CR to the end of the list
        _newMessage.Add(93); // ]
        _newMessage.Add(91); // [
        _newMessage.Add(10); // add LF to the end of the list
        _newMessage.Add(93); // ]

        controlBoardStream.Write (_newMessage.ToArray (), 0, _newMessage.Count);  // send command out
    }
    
    // fixed the float point value
    private byte [] FixpointProcesser (byte [] _data)
    {
        List<byte> _result = new List<byte>();
        byte _i = 0;
        bool _point = false;
        foreach (byte _d in _data)
        {
            if (_d == 46) _point = true;    // if find "."
            if (_point) _i++;   // start counting after find "."
            if (_i > 1 + Global_Output_FixpoitNum) break; // if Global_Output_FixpoitNum is 2, then stop loop if there are more than two decimal point number
            _result.Add(_d);
        }
        return _result.ToArray();
    }
    /*******************************
     * button funtion
     *******************************/
    public void BFMoveForwardDown() // move forward
    {
        moveForward = true;
    }
    public void BFMoveForwardUp()
    {
        moveForward = false;
    }

    public void BFMoveBackwardDown()    // move backward
    {
        moveBackward = true;
    }
    public void BFMoveBackwardUp()
    {
        moveBackward = false;
    }

    public void BFMoveUpDown()    // move Up
    {
        moveUp = true;
    }
    public void BFMoveUpUp()
    {
        moveUp = false;
    }

    public void BFMoveDownDown()    // move Dwon
    {
        moveDown = true;
    }
    public void BFMoveDownUp()
    {
        moveDown = false;
    }
    public void BFTurnLeftDown()    // turn left
    {
        turnLeft = true;
    }
    public void BFTurnLeftUp()
    {
        turnLeft = false;
    }

    public void BFTurnRightDown()    // turn right
    {
        turnRight = true;
    }
    public void BFTurnRightUp()
    {
        turnRight = false;
    }
    public void BFCamUpDown()
    {
        camUp = true;
    }
    public void BFCamUpUp()
    {
        camUp = false;
    }
    public void BFCamDownDown()
    {
        camDown = true;
    }
    public void BFCamDownUp()
    {
        camDown = false;
    }
    public void Light_0_On()
    {
        Light_0 = true;
    }
    public void Light_0_Off()
    {
        Light_0 = false;
    }
    public void Light_1_On()
    {
        Light_1 = true;
    }
    public void Light_1_Off()
    {
        Light_1 = false;
    }

    public void BFResetMode()
    {
        Reset_Mode = true;
        SendCommandToControlBoard(_resetMode: Reset_Mode);
    }
    public void BFResetModeDone()
    {
        Reset_Mode = Reset_Left = Reset_Right = Reset_Middle =false;
        SendCommandToControlBoard(_resetMode: Reset_Mode);
    }
    public void BFResetLeft()
    {
        if (Reset_Mode)
        {
            Reset_Left = !Reset_Left;
            SendCommandToControlBoard(_resetL: Reset_Left);
        }
        else Reset_Left = false;
    }
    public void BFResetRight()
    {
        if (Reset_Mode)
        {
            Reset_Right = !Reset_Right;
            SendCommandToControlBoard(_resetR: Reset_Right);
        }
        else Reset_Right = false;
    }
    public void BFResetMiddle()
    {
        if (Reset_Mode)
        {
            Reset_Middle = !Reset_Middle;
            SendCommandToControlBoard(_resetM: Reset_Middle);
        }
        else Reset_Middle = false;
    }
    /*******************************
    * end
    *******************************/

    // connection hart beat health test
    private bool IsConnected ()
    {
        try
        {
            return !(controlBoardTcp.Client.Poll(1, SelectMode.SelectRead) && controlBoardTcp.Available == 0);
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public void StartConnect()
    {
        ConnectToControlBoard();
        ConnectToCam();
    }

    private void ConnectToCam ()
    {

    }

    private void ConnectToControlBoard()
    {
        if (controlBoardSocketReadyFlag) return;

        string _host = controlBoardHost;
        int _port = controlBoardPort;

        try
        {
            controlBoardTcp = new TcpClient(); // client initialize
            controlBoardTcp.Connect(_host, _port);

            controlBoardStream = controlBoardTcp.GetStream(); //  steam initinalize
            
        }
        catch (Exception _ex)
        {
            Debug.Log("Socket error: " + _ex.Message);
        }
        finally
        {
            if (controlBoardTcp.Connected) controlBoardSocketReadyFlag = true;
        }
        
    }

    private void OnIncomingData(byte[] _dataIn)
    {
        int _state = 0;
        foreach(byte _d in _dataIn)
        {
            if (_d == 0) continue;

            switch(_state)
            {
                case 0:
                    if (_d == 13)    // if received byte is CR
                    {
                        _state = 10;
                        continue;
                    }
                    else
                    {
                        receiveList.Add(_d);
                    }
                    break;
                case 10:
                    if (_d == 10)    // if received LF then is correct data format
                    {
                        string receivedString = System.Text.Encoding.UTF8.GetString(receiveList.ToArray ());    // transfer to string
                        Debug.Log(receivedString);

                        receiveList = new List<byte>(); // clear cache
                        _state = 0;
                    }
                    else
                    {
                        Debug.Log("Error: Not sending correct Data format.");
                        receiveList = new List<byte>();
                        _state = 0;
                    }
                    break;
            }
        }
    }
}
