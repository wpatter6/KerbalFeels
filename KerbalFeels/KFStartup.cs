using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFeels
{
    //Start up right away and attach to events
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    class KFStartup : MonoBehaviour
    {
        bool _appLauncherInit = false;
        KFGUI _gui;
        KFRepeater _repeater;

        public void Awake()
        {
            KFUtil.Log("Instantly Awake");

            var eventsHandler = new KFEvents();
            eventsHandler.InitializeEvents();

            _gui = new KFGUI();
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            KFConfig.Init();

            KFConfig.Repeater = new KFRepeater();
        }

        private void OnGUIAppLauncherReady()
        {
            if (!_appLauncherInit)
            {
                KFUtil.Log("OnGUIAppLauncherReady");
                if (ApplicationLauncher.Ready)
                {
                    KFConfig.AppButton = ApplicationLauncher.Instance.AddModApplication(
                        onAppLaunchToggleOn,
                        onAppLaunchToggleOff,
                        onAppLaunchHoverOn,
                        onAppLaunchHoverOff,
                        onAppLaunchEnable,
                        onAppLaunchDisable,
                        ApplicationLauncher.AppScenes.ALWAYS,
                        (Texture)GameDatabase.Instance.GetTexture("KerbalFeels/Icons/KFIcon", true)
                    );
                    _appLauncherInit = true;
                }
            }
        }

        private void onAppLaunchDisable() 
        {
            KFUtil.Log("onAppLaunchDisable");
        }
        private void onAppLaunchEnable() 
        {
            KFUtil.Log("onAppLaunchEnable");
        }
        private void onAppLaunchHoverOff() 
        { 
            KFUtil.Log("onAppLaunchHoverOff"); 
        }
        private void onAppLaunchHoverOn() 
        { 
            KFUtil.Log("onAppLaunchHoverOn"); 
        }
        private void onAppLaunchToggleOff()
        {
            KFUtil.Log("onAppLaunchToggleOff");
            _gui.Hide();
        }
        private void onAppLaunchToggleOn()
        {
            KFUtil.Log("onAppLaunchToggleOn");
            _gui.ShowCrewDialog(FlightGlobals.ActiveVessel);
        }
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    class KFGameStartup : MonoBehaviour
    {
        //private KFGUI _gui;
        //private bool _init = false;
        public void Start()
        {
            KFUtil.Log("SpaceCentre Start");
            //KFConfig.Repeater.BeginRepeatingCheck();

            //if (!_init)
            //{
            //    _gui = new KFGUI();

            //    _init = true;
            //}

            //_gui.ShowCrewDialog();
        }
    }
    [KSPAddon(KSPAddon.Startup.TrackingStation, true)]
    class KFGameTrackingStation : MonoBehaviour
    {
        public void Start()
        {
            KFUtil.Log("TrackingStation Start");
            KFUtil.Log("UT: " + KFConfig.CurrentTime);
            KFConfig.Repeater.BeginRepeatingCheck();
        }
    }
    [KSPAddon(KSPAddon.Startup.Flight, true)]
    class KFGameFlilght : MonoBehaviour
    {
        public void Start()
        {
            KFUtil.Log("Flight Start");
            KFUtil.Log("UT: " + KFConfig.CurrentTime);
            KFConfig.Repeater.BeginRepeatingCheck();
        }
    }
}
