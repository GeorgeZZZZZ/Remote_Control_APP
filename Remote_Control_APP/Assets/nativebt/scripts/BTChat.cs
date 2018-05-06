using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

using leithidev.unityassets.nativebt.android.entities;

public class BTChat : MonoBehaviour
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
  }

  private void OnBTDeviceConnectingFailed(LWBluetoothDevice device)
  {
    this._connectedToText.text = "Connecting to " + device.GetName() + " failed!";
  }

  private void OnBtDeviceConnected(LWBluetoothDevice device)
  {
    this._connectedTo = device;
    this._connectedToText.text = device.GetName();
    this._btnDisconnect.gameObject.SetActive(true);
    this._btnPairedDevices.gameObject.SetActive(false);
  }

  private void OnBtDeviceDisconnected(LWBluetoothDevice device)
  {
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
