using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System;
using System.Text;
using UnityEngine.UI;

public class TCPIPConnenction : MonoBehaviour {

    public int Global_Output_FixpoitNum = 2;
    public bool Err_0_CommunicationError = false;
    public bool controlBoardSocketReadyFlag;

    private float smallestNum;
    private TcpClient controlBoardTcp;
    private NetworkStream controlBoardStream;

    public InputField Last_word_of_Host;
    private string Prehost = "192.168.80.";
    public string controlBoardHost;
    
    public int controlBoardPort = 23;


    public delegate void _newMessage(Robot_State _message);
    public event _newMessage New_Receive_Message_Event;


    public delegate void _socketReady();
    public delegate void _socketFailed(string _s);
    public event _socketReady Socket_Connected_Event;
    public event _socketFailed Socket_Fail_Event;
    public event _socketReady Socket_Disconnected_Event;

    // Use this for initialization
    void Start ()
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

        if (Last_word_of_Host != null)
        {
            Last_word_of_Host.text = "240";
            Last_word_of_Host.onValueChanged.AddListener(delegate { IP_Field_Changed_Handle(); });
        }
    }
    private void IP_Field_Changed_Handle ()
    {
        try
        {
            if (int.Parse(Last_word_of_Host.text) > 255) Last_word_of_Host.text = "255";
        }
        catch (FormatException) // if there is no number in the field
        {
            Last_word_of_Host.text = "0";
        }
    }
    float _disconnect_timer = 0;
    private void FixedUpdate()
    {
        if (_disconnect_timer > 0) _disconnect_timer -= Time.deltaTime;
        if (_tryToConnect)  // connect to tcpip server
        {
            if (controlBoardSocketReadyFlag)
            {
                if (_disconnect_timer <= 0)
                {
                    DisconnectFromControlBoard();
                    _disconnect_timer = 0.6f;   // delay for 0.6s
                }
            }
            else if (!controlBoardSocketReadyFlag && _disconnect_timer <= 0)
            {
                ConnectToControlBoard();
                _tryToConnect = false;
            }
        }

        if (_tryToDisconnect)  // disconnect from tcpip server
        {
            DisconnectFromControlBoard();
            _tryToDisconnect = _tryToConnect = false;
            _disconnect_timer = 0;
        }

        bool _isConnected = false;
        if (controlBoardSocketReadyFlag)
        {
            Err_0_CommunicationError = false;
            _isConnected = IsConnected(); //  connection health check
        }

        if (!_isConnected && controlBoardSocketReadyFlag)   // if server disconnect
        {
            DisconnectFromControlBoard();
        }

        // message receive function
        if (!_isConnected) return;  // if not connected
        if (!controlBoardStream.DataAvailable) return;  // if no data coming

        byte[] _buffer = new byte [255];
        List<byte> inData = new List<byte>();

        // get new message
        int _len = controlBoardStream.Read(_buffer, 0, _buffer.Length);
        for (int _i = 0; _i < _len; _i++) inData.Add(_buffer[_i]);
        
        bool _stateValid;
        Receive_Data(inData, out _stateValid, out getRobotState);
        
        if (!_stateValid) return;   // if message not ready
        
        Send_New_Message_To_Event(getRobotState);
    }
    
    private void OnDestroy()
    {
        // turn off tcp connect before destory this script, for example when close app.
        // otherwise may cause target device still remain connection after app closed.
        if (!controlBoardSocketReadyFlag) return;
        controlBoardStream.Close();
        controlBoardTcp.Close();
    }

    bool _tryToConnect;
    public void StartConnect()
    {
        _tryToConnect = true;
        //DisconnectFromControlBoard();
        //ConnectToControlBoard();
    }

    bool _tryToDisconnect;
    public void StartDisconnect()
    {
        _tryToDisconnect = true;
        //DisconnectFromControlBoard();
        //ConnectToControlBoard();
    }

    // connection hart beat health test
    private bool IsConnected()
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

    private void DisconnectFromControlBoard()
    {
        Err_0_CommunicationError = true;
        Clear_Receive_Data_Cache_Value();   // clear receive data analysis cache value

        if (!controlBoardSocketReadyFlag) return;
        Debug.Log("Error: Control Board Disconnect");
        controlBoardSocketReadyFlag = false;
        controlBoardStream.Close();
        controlBoardTcp.Close();

        if (Socket_Connected_Event != null)
            Socket_Disconnected_Event();   // send disconnect event
    }

    private void ConnectToControlBoard()
    {
        if (controlBoardSocketReadyFlag) return;

        if (Last_word_of_Host != null) controlBoardHost = Prehost + Last_word_of_Host.text;
        else controlBoardHost = Prehost + "240";

        string _host = controlBoardHost;
        int _port = controlBoardPort;

        try
        {
            controlBoardTcp = new TcpClient(); // client initialize
            //controlBoardTcp.BeginConnect(_host, _port, null, null);
            controlBoardTcp.Connect(_host, _port);
            controlBoardStream = controlBoardTcp.GetStream(); //  stream initinalize
        }
        catch (Exception _ex)
        {
            Debug.Log("Socket error: " + _ex.Message);
            if(Socket_Fail_Event != null)
                Socket_Fail_Event(_ex.Message); // sent connection fail event
        }
        finally
        {
            if (controlBoardTcp.Connected)
            {
                controlBoardSocketReadyFlag = true;
                if (Socket_Connected_Event != null)
                    Socket_Connected_Event();   // send connect event
            }
        }

    }

    public void SendCommandToControlBoard(float? _X = null, float? _Y = null, float? _Z = null
        , bool? _Grab = null, int? _Gesture = null
        , bool? _GestureDemo = null
        , bool? _HandEnable = null
        , int? CNCMove = null
        )
    {
        if (!controlBoardSocketReadyFlag)
        {
            Debug.Log("Error: Not connect to controlboard yet.");
            return;
        }

        List<byte> _newMessage = new List<byte>();    //  initinalize a list

        if (_X != null)
        {
            byte[] _xAxis = FixpointProcesser(Encoding.ASCII.GetBytes(_X.ToString())); //  transfer to ASCII

            _newMessage.Add(40);    // (
            _newMessage.Add(88);    // X
            _newMessage.Add(41);    // )
            _newMessage.AddRange(_xAxis); // move on x axis
            _newMessage.Add(59);    // ;
        }
        if (_Y != null)
        {
            byte[] _yAxis = FixpointProcesser(Encoding.ASCII.GetBytes(_Y.ToString()));

            _newMessage.Add(40);    // (
            _newMessage.Add(89);    // Y
            _newMessage.Add(41);    // )
            _newMessage.AddRange(_yAxis); // move on y axis
            _newMessage.Add(59);    // ;
        }
        if (_Z != null)
        {
            byte[] _zAxis = FixpointProcesser(Encoding.ASCII.GetBytes(_Z.ToString()));

            _newMessage.Add(40);    // (
            _newMessage.Add(90);    // Z
            _newMessage.Add(41);    // )
            _newMessage.AddRange(_zAxis); // move on z axis
            _newMessage.Add(59);    // ;
        }


        if (_Grab != null)
        {
            byte _command;
            if (_Grab == true) _command = 49;
            else _command = 48;

            _newMessage.Add(40);    // (
            _newMessage.Add(71);    // G
            _newMessage.Add(82);    // R
            _newMessage.Add(65);    // A
            _newMessage.Add(66);    // B
            _newMessage.Add(41);    // )
            _newMessage.Add(_command); // Open or close Grab
            _newMessage.Add(59);    // ;
        }

        if (_Gesture != null)
        {
            byte[] _command = Encoding.ASCII.GetBytes(_Gesture.ToString());

            _newMessage.Add(40);    // (
            _newMessage.Add(71);    // G
            _newMessage.Add(69);    // E
            _newMessage.Add(83);    // S
            _newMessage.Add(84);    // T
            _newMessage.Add(41);    // )
            _newMessage.AddRange(_command); // Num of gesture
            _newMessage.Add(59);    // ;
        }

        if (_GestureDemo != null)
        {
            byte _command;
            if (_GestureDemo == true) _command = 49;
            else _command = 48;

            _newMessage.Add(40);    // (
            _newMessage.Add(72);    // H
            _newMessage.Add(68);    // D
            _newMessage.Add(101);   // e
            _newMessage.Add(109);   // m
            _newMessage.Add(41);    // )
            _newMessage.Add(_command); // Strat or stop demo
            _newMessage.Add(59);    // ;
        }

        if (_HandEnable != null)
        {
            byte _command;
            if (_HandEnable == true) _command = 49;
            else _command = 48;

            _newMessage.Add(40);    // (
            _newMessage.Add(72);    // H
            _newMessage.Add(69);    // E
            _newMessage.Add(78);    // N
            _newMessage.Add(66);    // B
            _newMessage.Add(41);    // )
            _newMessage.Add(_command); // Enable or disable hand
            _newMessage.Add(59);    // ;
        }

        if (CNCMove != null)
        {
            byte[] _command = FixpointProcesser(Encoding.ASCII.GetBytes(CNCMove.ToString())); //  transfer to ASCII

            _newMessage.Add(40);    // (
            _newMessage.Add(67);    // C
            _newMessage.Add(78);    // N
            _newMessage.Add(67);    // C
            _newMessage.Add(41);    // )
            _newMessage.AddRange(_command); // Num of gesture
            _newMessage.Add(59);    // ;
        }

        _newMessage.Add(91); // [
        _newMessage.Add(13); // add CR to the end of the list
        _newMessage.Add(93); // ]
        _newMessage.Add(91); // [
        _newMessage.Add(10); // add LF to the end of the list
        _newMessage.Add(93); // ]

        controlBoardStream.Write(_newMessage.ToArray(), 0, _newMessage.Count);  // send command out
    }

    // fixed the float point value
    private byte[] FixpointProcesser(byte[] _data)
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

    /***************************
     * receive functions
     ***************************/

    public struct Robot_State
    {
        public bool? Hand_Enabled;
        public bool? Hand_Enb_Err;
        public bool? Grab_State;
        public int? Gesture_State;
        public bool? Hand_Mov_Err;
        public bool? Robot_Mov_Err;
    };

    private void Clear_Receive_Data_Cache_Value()
    {
        rdState = 0;
        rdTag = null;
        rdCache.Clear();
        getRobotState = new Robot_State();
    }
    private int rdState = 0; //receive data state
    private List<byte> rdCache = new List<byte> ();
    private string rdTag = null;
    private Robot_State getRobotState;
    private void Receive_Data(List<byte> rawData, out bool StateValid, out Robot_State newState)
    {
        Robot_State _r = new Robot_State ();

        StateValid = false;

        int _i = 0;
        float? _value = null;
        int? _errState = null;
        int _totalLength = 10;
        while (_i < rawData.Count)
        {
            switch (rdState)
            {
                case -50: // value not match error
                    Debug.Log("Receive data error state: " + _errState + ", content value format not match!!");
                    rdState = -10;
                    break;
                case -40: // cache leaking error
                    Debug.Log("Receive data error state: " + _errState + ", cache leaked!!");
                    rdState = -10;
                    break;
                case -30: // tag error
                    Debug.Log("Receive data error state: " + _errState + ", tag format not match!!");
                    rdState = -10;
                    break;

                case -20: // format not match error
                    Debug.Log("Receive data error state: " + _errState + ", format error!!");
                    rdState = -10;
                    break;

                case -10: // data error
                    _i = rawData.Count + 1;
                    _r = new Robot_State();
                    Clear_Receive_Data_Cache_Value();   // clear all cache and go back state 0
                    break;
                case 0:
                    _errState = rdState;    // incase if error occur
                    if (rawData[_i] == 40) // if read (
                    {
                        _i++;
                        rdTag = null;    // clear tag to provent pollution for second cycle comparison
                        _value = null;  // clear to provent pollution
                        rdCache.Clear();    // clear cache for very next using
                        rdState = 10;
                    }
                    else if (rawData[_i] == 91) // if read [
                    {
                        _i++;
                        rdState = 100;
                    }
                    else rdState = -20;
                    break;
                case 10:
                    _errState = rdState;    // incase if error occur

                    // if not read ) and value not too long and no error
                    while (rawData[_i] != 41 && rdCache.Count < _totalLength && rdState == 10)
                    {
                        // if data is a english letter, capital and lower case
                        if ((rawData[_i] >= 65 && rawData[_i] <= 90) || (rawData[_i] >= 97 && rawData[_i] <= 122))
                        {
                            rdCache.Add(rawData[_i]);
                            _i++;
                            if (_i >= rawData.Count) break; // only do next loop if not finish reading the data
                        }
                        else rdState = -30;   
                    }

                    if (rdCache.Count >= _totalLength)
                    {
                        rdState = -30; // if cache value too long
                        break;
                    }

                    if (_i >= rawData.Count) break; // protect next line
                    if (rawData[_i] == 41) // if read )
                    {
                        _i++;
                        rdTag = Encoding.ASCII.GetString(rdCache.ToArray());
                        rdCache.Clear();    // clear cache for very next using
                        rdState = 20;
                    }
                    break;
                case 20: // taking content data
                    _errState = rdState;    // incase if error occur

                    // if not read ; and value not too long and no error
                    while (rawData[_i] != 59 && rdCache.Count < _totalLength && rdState == 20) 
                    {
                        // if is reading number and . and -
                        if ((rawData[_i] >= 45 && rawData[_i] <= 46) || (rawData[_i] >= 48 && rawData[_i] <= 57))
                        {
                            rdCache.Add(rawData[_i]);
                            _i++;
                            if (_i >= rawData.Count) break; // only do next loop if not finish reading the data
                        }
                        else rdState = -50;
                    }

                    if (rdCache.Count >= _totalLength)
                    {
                        rdState = -50; // if cache value too long
                        break;
                    }

                    if (_i >= rawData.Count) break; // protect next line
                    if (rawData[_i] == 59)  // if read ;
                    {
                        try 
                        {
                            _value = float.Parse(Encoding.ASCII.GetString(rdCache.ToArray()));
                        }
                        catch(FormatException _e)
                        {
                            Debug.Log("Receive Data value converting error: " + _e);
                            rdState = -40;
                        }

                        if (rdState == 20) rdState = 30;    // if no err go next
                    }
                    break;
                case 30:
                    _errState = rdState;    // incase if error occur
                    if (rdTag == "Gest")    // gesture type
                    {
                        _r.Gesture_State = (int)_value;
                        if (_r.Gesture_State < 0 || 1000 < _r.Gesture_State) rdState = -50;
                    }
                    else if (rdTag == "HEnb")   // hand enable
                    {
                        if (_value == 1) _r.Hand_Enabled = true;
                        else if (_value == 0) _r.Hand_Enabled = false;
                        else rdState = -50;
                    }
                    else if (rdTag == "HEnbErr")    // hand enable error
                    {
                        if (_value == 1) _r.Hand_Enb_Err = true;
                        else if (_value == 0) _r.Hand_Enb_Err = false;
                        else rdState = -50;
                    }
                    else if (rdTag == "Grab")   // hand grab state
                    {
                        if (_value == 1) _r.Grab_State = true;
                        else if (_value == 0) _r.Grab_State = false;
                        else rdState = -50;
                    }
                    else if (rdTag == "HMovErr")    // hand move error
                    {
                        if (_value == 1) _r.Hand_Mov_Err = true;
                        else if (_value == 0) _r.Hand_Mov_Err = false;
                        else rdState = -50;
                    }
                    else if (rdTag == "RMovErr")    // robot move error
                    {
                        if (_value == 1) _r.Robot_Mov_Err = true;
                        else if (_value == 0) _r.Robot_Mov_Err = false;
                        else rdState = -50;
                    }

                    if (rdState == 30)
                    {
                        _i++;
                        rdState = 0; // if evey thing fine then turn to start
                    }
                    break;

                case 100:
                    _errState = rdState;    // incase if error occur
                    if (rawData[_i] == 13)
                    { // if read CR
                        _i++;
                        rdState = 110;
                    }
                    else if (rawData[_i] == 10)
                    { // if read LF
                        _i++;
                        rdState = 120;
                    }
                    else rdState = -20;
                    break;
                case 110:
                    _errState = rdState;    // incase if error occur
                    if (rawData[_i] == 93)
                    { // if read ]
                        _i++;
                        rdState = 0;
                    }
                    else rdState = -20;
                    break;
                case 120:
                    _errState = rdState;    // incase if error occur
                    if (rawData[_i] == 93)
                    { // if read ]
                        StateValid = true;
                        _i++;
                        rdState = 0;
                    }
                    else rdState = -20;
                    break;
            }
        }
        newState = _r;
    }

    private void Send_New_Message_To_Event (Robot_State RobotState)
    {
        if (New_Receive_Message_Event != null)
            New_Receive_Message_Event(RobotState);
    }
    
}
