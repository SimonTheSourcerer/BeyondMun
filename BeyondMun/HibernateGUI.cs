using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Timers;
using KSP.IO;
using KSP.UI.Screens;
using BeyondMun;

[KSPAddon(KSPAddon.Startup.EveryScene, false)]
public class HibernateGUI : MonoBehaviour
{
    internal static string assetFolder = "BeyondMun/Assets/";

    public static ModuleHibernate callingModule;

    private static String hibernationTime = "1";
    private static bool breakWarp = false;

    private static ApplicationLauncherButton CivPopButton = null;
    static bool CivPopGUIOn = false;
    internal bool CivPopTooltip = false;
    private GUIStyle _windowstyle, _labelstyle;
    private bool hasInitStyles = false;

    private static ApplicationLauncherButton appButton = null;

    /// <summary>
    /// Awake this instance.  Pre-existing method in Unity that runs before KSP loads.
    /// </summary>
    public void Awake()
    {
        //Debug.Log (debuggingClass.modName + "Starting Awake()");//What I am using to debug
        DontDestroyOnLoad(this);
        GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);//when AppLauncher can take apps, give it OnAppLauncherReady (mine)
        GameEvents.onGUIApplicationLauncherDestroyed.Add(OnAppLauncherDestroyed);//Not sure what this does
    }

    public void OnAppLauncherDestroyed()
    {
        if (appButton != null)
        {
            Cancel();
            ApplicationLauncher.Instance.RemoveApplication(appButton);
        }
    }


    /// <summary>
    /// Raises the app launcher ready event.  Method to create an app button on the AppLauncher, as well as tell
    /// what/how the GUI is loaded.
    /// </summary>
    public void OnAppLauncherReady()
    {
        InitStyle();
        string iconFile = "HibernateIcon";//This is the name of the file that stores the mod's icon to be used in the appLauncher
        if (HighLogic.LoadedScene == GameScenes.SPACECENTER && appButton == null)
        {//i.e. if running for the first time
            appButton = ApplicationLauncher.Instance.AddModApplication(
                OnToggleTrue,                           //Run OnToggleTrue() when user clicks button
                Cancel,                          //Run OnToggleFalse() when user clicks button again
                null, null, null, null,                 //do nothing during hover, exiting, enable/disable
                ApplicationLauncher.AppScenes.ALWAYS,   //When to show applauncher/GUI button
                GameDatabase.Instance.GetTexture(assetFolder + iconFile, false));//where to look for mod applauncher icon
                                                                                //Debug.Log (debuggingClass.modName + "Finishing Awake()");//What I am using to debug
        }
        CivPopGUIOn = false;
    }


    /// <summary>
    /// Presumably what to do when the user opens/clicks the button.  Called from OnAppLauncherReady.
    /// </summary>
    public static void OnToggleTrue()
    {
        //Debug.Log (debuggingClass.modName + "Starting OnToggleTrue()");
        CivPopGUIOn = true;//turns on flag for GUI
                            //Debug.Log (debuggingClass.modName + "Turning on GUI");
    }

    public static void StartHibernate()
    {
        try
        {
            float time = (float)Int32.Parse(hibernationTime);
            if (time > 0)
            {
                time = time * ModuleHibernate.TIME_ONE_HOUR * 6f; // days
                callingModule.GoHibernate(time);
            }
        }
        catch (FormatException e)
        {
            Console.WriteLine(e.Message);
        }

        CivPopGUIOn = false;
    }

    public static void Cancel()
    {
        CivPopGUIOn = false;
    }

    /// <summary>
    /// I'm not sure what this is for...but it was already here and it seems to work.
    /// </summary>
    private void InitStyle()
    {
        _windowstyle = new GUIStyle(HighLogic.Skin.window);
        _labelstyle = new GUIStyle(HighLogic.Skin.label);
        hasInitStyles = true;
    }

    /// <summary>
    /// OnGUI() is called by the game and every time it refreshes the GUI.  I just need it to check if the GUI is
    /// enabled and if it is, show it.
    /// </summary>
    public void OnGUI()
    {//Executes code whenever screen refreshes.  Extension to enable use of button along main bar on top-right of screen.
        if (CivPopGUIOn)
        {
            PopulationManagementGUI();//If button is enabled, display rectangle.
        }//end if
    }//end OnGui extension

    /// <summary>
    /// This method controls how the window actually looks when the HUD window is displayed.
    /// </summary>
    void PopulationManagementGUI()
    {
        GUI.BeginGroup(new Rect(Screen.width / 2 - 250, Screen.height / 2 - 250, 500, 500));
        GUILayout.BeginVertical("box");

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Probe Hibernation", style);

        GUILayout.BeginHorizontal();           
        GUILayout.Label("Sleep for days: ");
        hibernationTime = GUILayout.TextField(hibernationTime, 12);
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        breakWarp = GUILayout.Toggle(breakWarp, " Break warp on awake");
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel", GUILayout.Width(100f)))
        {
            Cancel();
        }
        if (GUILayout.Button("Hibernate", GUILayout.Width(100f)))
        {
            StartHibernate();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUI.EndGroup();
    }
}