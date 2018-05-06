using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Manually_Send_Command : MonoBehaviour {

    public Keywords_analysis ka;
    public InputField Execute_CNC_Program_Num;

    public void Button_Connect_Robot ()
    {
        if (ka == null) return;
        ka.recognized_str.Add("连接机器人");
    }
    public void Button_Disconnect_Robot()
    {
        if (ka == null) return;
        ka.recognized_str.Add("断开机器人");
    }
    public void Button_Enable_Hand()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人使能");
    }
    public void Button_Disable_Hand()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人关闭使能");
    }
    public void Button_Show_Hand_Demo()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人演示");
    }
    public void Button_Stop_Hand_Demo()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人停止演示");
    }
    public void Button_Show_Gesture_1()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人给我1");
    }
    public void Button_Show_Gesture_2()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人给我2");
    }
    public void Button_Show_Gesture_3()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人给我3");
    }
    public void Button_Show_Gesture_4()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人给我4");
    }
    public void Button_Show_Gesture_5()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人给我5");
    }
    public void Button_Show_Gesture_6()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人给我6");
    }
    public void Button_Show_Gesture_Fist()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人拳头");
    }
    public void Button_Show_Gesture_Plam()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人手掌");
    }
    public void Button_Show_Gesture_ThumbUp()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人超级棒");
    }
    public void Button_Show_Gesture_ComeOnBaby()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人勾引");
    }
    public void Button_Grab_Close()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人抓紧");
    }
    public void Button_Grab_Release()
    {
        if (ka == null) return;
        ka.recognized_str.Add("机器人松开");
    }

    public void Button_Execute_CNC_Program()
    {
        if (ka == null || Execute_CNC_Program_Num == null) return;
        try
        {
            int.Parse(Execute_CNC_Program_Num.text);
        }
        catch (System.FormatException) // if there is no number in the field
        {
            Execute_CNC_Program_Num.text = "0";
        }
        ka.recognized_str.Add("启动" + Execute_CNC_Program_Num.text + "号程序");
    }
}
