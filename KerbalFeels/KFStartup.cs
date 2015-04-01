﻿using System;
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
        ApplicationLauncherButton _appButton = null;
        KFGUI _gui;

        public void Awake()
        {
            KFUtil.Log("Instantly Awake");

            var eventsHandler = new KFEvents();
            eventsHandler.InitializeEvents();

            _gui = new KFGUI();
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            KFConfig.Init();
        }

        private void OnGUIAppLauncherReady()
        {
            if (!_appLauncherInit)
            {
                KFUtil.Log("OnGUIAppLauncherReady");
                if (ApplicationLauncher.Ready)
                {
                    _appButton = ApplicationLauncher.Instance.AddModApplication(
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

        private void onAppLaunchDisable() { }
        private void onAppLaunchEnable() { }
        private void onAppLaunchHoverOff() { }
        private void onAppLaunchHoverOn() { }
        private void onAppLaunchToggleOff() { _gui.Hide();  }
        private void onAppLaunchToggleOn() 
        {
            _gui.ShowCrewDialog(FlightGlobals.ActiveVessel);
        }
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    class KFGameStartup : MonoBehaviour
    {
        private KFGUI _gui;
        private bool _init = false;
        public void Start()
        {
            KFUtil.Log("SpaceCentre Start");

            if (!_init)
            {
                _gui = new KFGUI();
                var repeater = new KFRepeater();
                repeater.BeginRepeatingCheck();

                _init = true;

            }

            //_gui.ShowCrewDialog();
        }
    }
}
