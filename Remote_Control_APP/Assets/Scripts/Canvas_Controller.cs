using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Canvas_Controller : MonoBehaviour {
    public Canvas[] Canvas_Group;

    private int Page_Assign;
    private int total_pages;
    // initialize before game start
    private void Start()
    {
        total_pages = Canvas_Group.Length - 1;
        // turn off every page expect first one
        for (int i = 1; i <= total_pages; i++) Canvas_Group[i].enabled = false;
        Page_Assign = 0;
    }

    private void Change_Page()
    {
        Page_Assign = Mathf.Clamp(Page_Assign, 0, total_pages);
        if (Canvas_Group == null || Page_Assign > total_pages) return;
        int i = 0;
        foreach (Canvas _a in Canvas_Group)
        {
            if (i == Page_Assign)   _a.enabled = true;
            else _a.enabled = false;
            i++;
        }
    }

    public void Button_Page_Previous()
    {
        Page_Assign --;
        Change_Page();
    }
    public void Button_Page_Next()
    {
        Page_Assign ++;
        Change_Page();
    }
}
