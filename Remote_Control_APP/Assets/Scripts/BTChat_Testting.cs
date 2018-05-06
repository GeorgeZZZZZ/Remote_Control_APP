using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

using leithidev.unityassets.nativebt.android.entities;

public class BTChat_Testting : MonoBehaviour
{
    public Text _connectedToText;
    public Text _listeningOnText;
    public Text _chatText;
    public InputField _sendInputField;
    public Text _sendText;
    public PairedDeviceButton _pairedDeviceListButton;
    public RectTransform _pairedDeviceList;
    public RectTransform _btnDisconnect;
    public RectTransform _btnPairedDevices;
    public string uuid = "c1db6770-a359-11e6-80f5-76304dec7eb7";

    public bool Device_Connected;
    // Use this for initialization
    void Start()
    {
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTMessageReceived += OnMessageReceived;
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTMessageSent += OnMessageSent;
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTDeviceConnected += OnBtDeviceConnected;
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTDeviceDisconnected += OnBtDeviceDisconnected;
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTDeviceConnectingFailed += OnBTDeviceConnectingFailed;
        this._pairedDeviceList.gameObject.SetActive(false);
        this._btnDisconnect.gameObject.SetActive(false);

        //*

        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTPaired += BTEventsHandler_BTPaired;
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTPairingFailed += BTEventsHandler_BTPairingFailed;
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTPairingRequest += BTEventsHandler_BTPairingRequest;
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTAdapterDiscoveryStarted += BTEventsHandler_BTAdapterDiscoveryStarted;
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTAdapterDiscoveryFinished += BTEventsHandler_BTAdapterDiscoveryFinished;
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTAdapterDiscoveryCanceled += BTEventsHandler_BTAdapterDiscoveryCanceled;
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTDeviceFound += BTEventsHandler_BTDeviceFound;
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTDeviceConnecting += BTEventsHandler_BTDeviceConnecting;
        NativeBTRuntime.NBTR.BTHandler.BTEventsHandler.BTDeviceListening += BTEventsHandler_BTDeviceListening;
        //*/
    }

    private void BTEventsHandler_BTDeviceListening(string msg)
    {
        _chatText.text += "I'm listening :" + msg;
    }

    private void BTEventsHandler_BTDeviceConnecting(LWBluetoothDevice device)
    {
        _chatText.text += "\nConnecting to " + device.GetName();
    }

    public void Button_Connect_to_Device_HC06()
    {
        _chatText.text += "\nConnect Button clicked.";
        if (!NativeBTRuntime.NBTR.BTWrapper.GetBTAdapter().IsEnabled())
        {
            NativeBTRuntime.NBTR.BTWrapper.ShowBTEnableRequest();
        }
        else
        {
            IList<LWBluetoothDevice> _devices = NativeBTRuntime.NBTR.BTWrapper.GetPairedDevices();
            if (_devices.Count != 0)
            {
                foreach (LWBluetoothDevice _d in _devices)
                {
                    //if (_d.GetName().Contains("Philips"))
                    if (_d.GetName().Contains("HC-06"))
                    {
                        NativeBTRuntime.NBTR.BTWrapper.Connect(_d, this.uuid);
                    }
                }
            }
        }
    }

    private void BTEventsHandler_BTPaired(LWBluetoothDevice device)
    {
        _chatText.text += "\nDevice paired.";
        myDevice = device;
        IList<string> uuid_list = NativeBTRuntime.NBTR.BTWrapper.GetUUIDS(device);
        myDeviceUUID = uuid_list[0];
        //int a = NativeBTRuntime.NBTR.BTWrapper.GetUUIDS(newDevices[0]);
        _chatText.text += "\nDevice UUID is: ";
        foreach (string _u in uuid_list)
        {
            _chatText.text += "\n" + _u;
        }
        _chatText.text += "\nmyDeviceUUID is: " + myDeviceUUID;
    }

    private void BTEventsHandler_BTPairingFailed(LWBluetoothDevice device)
    {
        _chatText.text += "\nPair filed!!!";
    }

    private void BTEventsHandler_BTPairingRequest(LWBluetoothDevice device)
    {
        _chatText.text += "\nStarting pair ... ...";
    }

    //*
    LWBluetoothDevice myDevice;
    string myDeviceUUID;
    List<LWBluetoothDevice> newDevices = new List<LWBluetoothDevice>();
    int oldListCount = 0;
    private void Update()
    {
        /*
        if (device_connected)
            NativeBTRuntime.NBTR.BTWrapper.Send("asdasd" + System.Environment.NewLine);
        else if (device_connected == false && _connectedTo != null)
            NativeBTRuntime.NBTR.BTWrapper.Connect(_connectedTo, _connectedTo.GetHashCode().ToString());
        */
        if (oldListCount != newDevices.Count)
        {
            oldListCount = newDevices.Count;
            _chatText.text += "\nFound device" + ", list count " + newDevices.Count + ":\n" + "   " + newDevices[newDevices.Count - 1].GetName();
        }
    }
    public void Button_Try_Pair()
    {
        _chatText.text += "\nPair Button clicked.";
        if (newDevices.Count > 0)
        {
            _chatText.text += "\nReady to pair: " + newDevices[0];
            NativeBTRuntime.NBTR.BTWrapper.CreateBond(newDevices[0]);
        }
        else _chatText.text += "\nNo device in list, can't pair.";
    }
    private void BTEventsHandler_BTDeviceFound(LWBluetoothDevice device)
    {
        if (newDevices.Count > 0)
        {
            bool _contain = false;
            // if current device is in device then don't add in
            foreach(LWBluetoothDevice _d in newDevices)
            {
                if (_d.GetHashCode() == device.GetHashCode()
                    || _d.GetAddress() == device.GetAddress()) _contain = true;
            }
            if (!_contain) newDevices.Add(device);
        }
        else if (newDevices.Count == 0) newDevices.Add(device); // if device list is empty then add current one
        
    }

    private void BTEventsHandler_BTAdapterDiscoveryCanceled(IList<LWBluetoothDevice> devices)
    {
        _chatText.text += "\nCancel search.";
    }

    private void BTEventsHandler_BTAdapterDiscoveryFinished(IList<LWBluetoothDevice> devices)
    {
        _chatText.text += "\nFinished search.";
    }

    public void Button_Try_Search()
    {
        _chatText.text += "\nSearch Button clicked.";
        NativeBTRuntime.NBTR.BTWrapper.StartDiscoverDevices();
    }
    private void BTEventsHandler_BTAdapterDiscoveryStarted()
    {
        _chatText.text += "\nStart search for device?";
    }

    public void Button_Try_to_Send_0()
    {
        for (int _motorNum = 0; _motorNum < 16; _motorNum++)
            Send_Massage_To_Servo_Board(0x02, (byte)_motorNum, 0xf4, 0x01); // 0deg
    }
    public void Button_Try_to_Send_1()
    {
        for (int _motorNum = 0; _motorNum < 16; _motorNum++)
            Send_Massage_To_Servo_Board(0x02, (byte)_motorNum, 0xdc, 0x05);  // 90deg
    }
    public void Button_Try_to_Send_2()
    {
        for (int _motorNum = 0; _motorNum < 16; _motorNum++)
            Send_Massage_To_Servo_Board(0x02, (byte)_motorNum, 0xc4, 0x09); // 180deg
    }
    public void Button_Try_to_Speed_1()
    {
        for (int _motorNum = 0x01; _motorNum < 16; _motorNum++)
            Send_Massage_To_Servo_Board(0x01, (byte)_motorNum, 0x01, 0x00); // 0x01 9deg/s, 0x0a 90deg/s
    }
    public void Button_Try_to_Speed_2()
    {
        for (int _motorNum = 0x01; _motorNum < 16; _motorNum++)
            Send_Massage_To_Servo_Board(0x01, (byte)_motorNum, 0x14, 0x00); // 0x01 9deg/s, 0x0a 90deg/s, 0x14 180deg/s
    }
    public void Send_Massage_To_Servo_Board(byte _type, byte? _servoNum, byte? _value_Low, byte? _value_High)
    {
        byte[] _massage = new byte[5];
        _massage[0] = 0xFF;
        _massage[1] = _type;
        switch(_type)
        {
            case 0x01:
                if (_servoNum == null
                    || _value_Low == null)
                    return;
                _massage[2] = _servoNum.GetValueOrDefault();
                _massage[3] = _value_Low.GetValueOrDefault();
                _massage[4] = _value_High.GetValueOrDefault();
                break;
            case 0x02:
                if (_servoNum == null
                    || _value_Low == null
                    || _value_High == null)
                    return;
                _massage[2] = _servoNum.GetValueOrDefault();
                _massage[3] = _value_Low.GetValueOrDefault();
                _massage[4] = _value_High.GetValueOrDefault();
                break;
        }
        NativeBTRuntime.NBTR.BTWrapper.Send(_massage);
    }
    //*/
    /// <summary>
    /// 
    /// </summary>
    /// <param name="device"></param>

    private void OnBTDeviceConnectingFailed(LWBluetoothDevice device)
    {
        this._connectedToText.text = "Connecting to " + device.GetName() + " failed!";
    }

    private void OnBtDeviceConnected(LWBluetoothDevice device)
    {
        Device_Connected = true;
        _chatText.text += "\nDevice Connected.";
        this._connectedTo = device;
        this._connectedToText.text = device.GetName();
        this._btnDisconnect.gameObject.SetActive(true);
        this._btnPairedDevices.gameObject.SetActive(false);
    }

    private void OnBtDeviceDisconnected(LWBluetoothDevice device)
    {
        Device_Connected = false;
        _chatText.text += "\nDevice Disconnected!!!";
        this._connectedTo = null;
        this._connectedToText.text = "";
        this._btnDisconnect.gameObject.SetActive(false);
        this._btnPairedDevices.gameObject.SetActive(true);
    }

    private void OnMessageSent(string msg)
    {
      this._chatText.text += "\n" + NativeBTRuntime.NBTR.BTWrapper.GetBTAdapter().GetName() + ": " + msg;
    }

    private void OnMessageReceived(string msg)
    {
        _chatText.text += "\nNew message received!";
        this._chatText.text += "\n" + this._connectedTo.GetName() + ": " + msg;
    }

    public void OnDisconnectButtonClicked()
    {
       NativeBTRuntime.NBTR.BTWrapper.Disconnect();
    }

    public void OnSendButtonClicked()
    {
        string msg = this._sendInputField.text;
        this._sendInputField.text = "";

        NativeBTRuntime.NBTR.BTWrapper.Send(msg + System.Environment.NewLine);
    }
    public void OnPairedDeviceButtonClicked(LWBluetoothDevice btDevice)
    {
        NativeBTRuntime.NBTR.BTWrapper.Connect(btDevice, this.uuid);
        this._pairedDeviceList.gameObject.SetActive(false);
    }

    public void OnPairedDevicesClicked()
    {
        if (!NativeBTRuntime.NBTR.BTWrapper.GetBTAdapter().IsEnabled())
        {
            NativeBTRuntime.NBTR.BTWrapper.ShowBTEnableRequest();
        }
        else
        {
            IList<LWBluetoothDevice> devices = NativeBTRuntime.NBTR.BTWrapper.GetPairedDevices();
            this._pairedDeviceList.gameObject.SetActive(true);
            this.ClearPairedDevicesList();
            if (devices.Count == 0)
            {
                this._pairedDeviceList.gameObject.SetActive(false);
            }
            foreach (LWBluetoothDevice device in devices)
            {
                PairedDeviceButton pdb = Instantiate<PairedDeviceButton>(this._pairedDeviceListButton);
                pdb._device = device;
                pdb.GetComponent<Button>().GetComponentInChildren<Text>().text = device.GetName() + "|" + device.GetAddress();
                pdb.GetComponent<Button>().onClick.AddListener(() => OnPairedDeviceButtonClicked(pdb._device));
                pdb.transform.SetParent(this._pairedDeviceList);
            }
        }
    }

    private void ClearPairedDevicesList()
    {
        IList<GameObject> objsToDestroy = new List<GameObject>();
        for (int x = 0; x < this._pairedDeviceList.transform.childCount; x++)
        {
            objsToDestroy.Add(this._pairedDeviceList.transform.GetChild(x).gameObject);
        }

        foreach(GameObject go in objsToDestroy)
        {
            Destroy(go);
        }
    }

    public void OnListenClicked()
    {
        NativeBTRuntime.NBTR.BTWrapper.Disconnect();
        if (!NativeBTRuntime.NBTR.BTWrapper.GetBTAdapter().IsEnabled())
        {
            NativeBTRuntime.NBTR.BTWrapper.ShowBTEnableRequest();
        }
        else
        {
            NativeBTRuntime.NBTR.BTWrapper.Listen(true, this.uuid);
            this._listeningOnText.text = "Listen on: " + uuid;
        }
    }

    private LWBluetoothDevice _connectedTo;
}
