using UnityEngine;
using System.Collections;

using leithidev.unityassets.nativebt.android.core;

public class NativeBTRuntime {

  private static NativeBTRuntime _nbtr;

  public static NativeBTRuntime NBTR
  {
    get
    {
      if(NativeBTRuntime._nbtr == null)
      {
        NativeBTRuntime._nbtr = new NativeBTRuntime();
      }
      return NativeBTRuntime._nbtr;
    }
  }


  private NativeBTRuntime()
  {
    this._btHandler = new AndroidNativeBTHandler();
    this._btHandler.SetDelimeter(System.Environment.NewLine);
  }

  public AndroidNativeBTWrapper BTWrapper
  {
    get
    {
      if(this._btHandler != null)
      {
        return this._btHandler.BTWrapper;
      }
      return null;
    }
  }

  public AndroidNativeBTHandler BTHandler
  {
    get
    {
      return this._btHandler;
    }
  }
  private AndroidNativeBTHandler _btHandler;

}
