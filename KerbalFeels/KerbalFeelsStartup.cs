using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
using File = KSP.IO.File;

//using System.Data.SQLite;
//using Newtonsoft.Json.Linq;

namespace KerbalFeels
{
    //Start up right after the user has selected a save game to load
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class KerbalFeelsStartup : MonoBehaviour
    {
        private string _saveFolderName;

        public void Awake()
        {
            if (_saveFolderName == HighLogic.SaveFolder) return;//we've initialized for this save already
            KFUtil.Log("Awake");

            List<char> invalidChars = new List<char>(Path.GetInvalidFileNameChars());
            invalidChars.Add(' ');

            _saveFolderName = HighLogic.SaveFolder;


            var crewDbSaveFileName = String.Format("{0}{1}{2}", "KF_crew_", new string(HighLogic.SaveFolder.Where(x => !invalidChars.Contains(x)).ToArray()), ".cfg");
            var flightsDbSaveFileName = String.Format("{0}{1}{2}", "KF_flights_", new string(HighLogic.SaveFolder.Where(x => !invalidChars.Contains(x)).ToArray()), ".cfg");

            var flightsDbSaveFileNameAndPath = IOUtils.GetFilePathFor(this.GetType(), flightsDbSaveFileName);
            var crewDbSaveFileNameAndPath = IOUtils.GetFilePathFor(this.GetType(), crewDbSaveFileName);

            var eventsHandler = new KerbalFeelsEvents(crewDbSaveFileName, flightsDbSaveFileName, crewDbSaveFileNameAndPath, flightsDbSaveFileNameAndPath);
            eventsHandler.InitializeEvents();
            //RenderingManager.AddToPostDrawQueue(0, OnDrawGUI);
        }

        public void Start()
        {
            KFUtil.Log("Start");

        }
    }
}
