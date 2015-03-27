using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
using File = KSP.IO.File;
using Random = System.Random;

namespace KerbalFeels
{
    static class KFUtil
    {

        #region logging
        private const string _logAppender = "FEELS: ";

        public static void Log(string message)
        {
            Debug.Log(_logAppender + message);
        }

        public static void Log(object message)
        {
            Log(message.ToString());
        }

        public static void LogError(string message)
        {
            Debug.LogError(_logAppender + message);
        }
        #endregion

        #region general util
        public static string GetFirstName(String name)
        {
            return name.Substring(0, name.IndexOf(' '));
        }

        public static string GetFirstName(ProtoCrewMember crewMember)
        {
            return GetFirstName(crewMember.name);
        }

        public static string GetFeelsChangeText(FeelsChange change)
        {
            return String.Format("{0} likes {1} {2}", change.NewFeel.CrewMember, change.NewFeel.ToCrewMember, GetFeelsChangeText(change.NewFeel.Number - change.OldFeel.Number));
        }

        public static string GetFeelsChangeText(double change)
        {
            string text = "about the same";

            if (change > .1)
            {
                text = "more";
            }
            else if (change < -.1)
            {
                text = "less";
            }
            var abs = Math.Abs(change);
            if (abs > 5)
                text = "a lot " + text;
            else if (abs < 1 && abs > .1)
                text = "a little " + text;

            return text;
        }

        public static ConfigNode GetConfigNode(String name)
        {
            var feelsNode = new ConfigNode();

            if (HighLogic.CurrentGame.config.HasNode("FEELS"))
                feelsNode = HighLogic.CurrentGame.config.GetNode("FEELS");
            else 
                feelsNode = HighLogic.CurrentGame.config.AddNode("FEELS");

            if (feelsNode.HasNode(name))
                return feelsNode.GetNode(name);
            else
                return feelsNode.AddNode(name);
        }
        #endregion
        
        #region gui
        private static Rect windowPosition = new Rect(150, 150, 220, 240);
        private static GUIStyle guiStyle = null;

        public static void OnDrawGUI()
        {
            Log("OnDrawGUI");
            guiStyle = new GUIStyle(HighLogic.Skin.window);
            windowPosition = GUI.Window(1, windowPosition, FeelsChangedWindowGUI, "Kerbal Feels", guiStyle);
        }

        public static void FeelsChangedWindowGUI(int WindowID)
        {
            Log("FeelsChangedWindowGUI");
            Log(HighLogic.CurrentGame.config.HasNode("FEELS_CHANGE_TEXT").ToString());
            if (HighLogic.CurrentGame.config.HasNode("FEELS_CHANGE_TEXT"))
            {
                var text = HighLogic.CurrentGame.config.GetNode("FEELS_CHANGE_TEXT");

                if (text.HasNode("TEXT"))
                {

                    Log("text.nodes.count: " + text.nodes.Count.ToString());

                    GUILayout.BeginVertical();

                    foreach (ConfigNode node in text.nodes)
                    {
                        string str = node.GetValue("value");
                        Log("str: " + str);
                        GUILayout.Label(str);
                    }

                    if (GUILayout.Button("Ok", guiStyle, GUILayout.ExpandWidth(true)))//GUILayout.Button is "true" when clicked
                    {
                        RenderingManager.RemoveFromPostDrawQueue(0, OnDrawGUI);
                    }

                    GUILayout.EndVertical();

                    GUI.DragWindow(new Rect(0, 0, 10000, 20));
                }
            }
        }

        #endregion 
        
    }

    public struct FeelsChange
    {
        public Feels OldFeel;
        public Feels NewFeel;

        public FeelsChange(Feels oldFeel = new Feels(), Feels newFeel = new Feels())
        {
            OldFeel = oldFeel;
            NewFeel = newFeel;
        }
    }

    public struct Feels
    {
        public string CrewMember;
        public string ToCrewMember;
        public double Number;
        public FeelingTypes Type;

        public Feels(string crewMember, string toCrewMember, double num = 0, FeelingTypes type = FeelingTypes.Indifferent)
        {
            Number = num;
            Type = type;
            CrewMember = crewMember;
            ToCrewMember = toCrewMember;
        }
    }

    //negative values will lower the kerbal's level by one, positive will raise by one.
    public enum FeelingTypes
    {
        Hateful = -3,//raises stupidity and courage, can trigger a murder (maybe?)
        Bored = -2,//raises stupidity
        Scared = -1,//lowers courage
        Indifferent = 0,//does nothing
        Playful = 1,//reduces experience gained (maybe?)
        InLove = 2,//raises stupidity and courage
        Inspired = 3//raises experience gained (maybe?)
    } 
}
