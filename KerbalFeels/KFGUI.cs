using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFeels
{
    class KFGUI
    {
        private const int _queuePosition = 98;
        private const string _defaultTitle = "Kerbal Feels";
        private const string _guiTextKey = "FEELS_GUI_TEXT";

        private string _guiTextKeyUnique
        {
            get
            {
                return String.Format("{0}_{1}", _guiTextKey, _guiUID.ToString());
            }
        }

        private Rect windowPosition = new Rect(150, 150, 307, 300);
        private Rect sectionPosition = new Rect(0, 0, 300, 200);

        private Guid _guiUID;

        private GUIStyle guiStyle = null;
        private GUIStyle buttonStyle = null;
        private GUIStyle subButtonStyle = null;

        private Vector2 scrollPos = new Vector2();
        private Func<bool> buttonClickFunc = null;

        public KFGUI()
        {
            guiStyle = new GUIStyle(HighLogic.Skin.window);

            buttonStyle = new GUIStyle(guiStyle);
            buttonStyle.fontSize = 20;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            subButtonStyle = new GUIStyle(guiStyle);
            subButtonStyle.fontSize = 12;
            subButtonStyle.contentOffset = new Vector2(0, -9);
            subButtonStyle.alignment = TextAnchor.MiddleLeft;
            subButtonStyle.clipping = TextClipping.Overflow;

            _guiUID = Guid.NewGuid();
        }

        private void OnDrawGUI()
        {
            var title = _defaultTitle;
            var text = HighLogic.CurrentGame.config.GetNode(_guiTextKeyUnique);
            if (text.HasNode("TITLE"))
            {
                title = text.GetNode("TITLE").GetValue("value");
            }
            //Log("OnDrawGUI");
            windowPosition = GUI.Window(1, windowPosition, FeelsWindowGUI, title, guiStyle);
        }

        private void FeelsWindowGUI(int WindowID)
        {
            if (HighLogic.CurrentGame.config.HasNode(_guiTextKeyUnique))
            {
                var text = HighLogic.CurrentGame.config.GetNode(_guiTextKeyUnique);
           
                if (text.HasNode())
                {
                    GUILayout.BeginVertical();
                    foreach (ConfigNode node in text.nodes)
                    {
                        string val = node.GetValue("value");
                        switch (node.name)
                        {
                            case "TEXT":
                                GUILayout.Label(val);
                                break;
                            case "BUTTON": 
                                if (GUILayout.Button(val, subButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(20)))
                                {
                                    ShowKerbalDialog(val);
                                }
                                break;
                            case "SECTION_START":
                                GUILayout.BeginArea(sectionPosition, val, guiStyle);
                                break;
                            case "SECTION_END":
                                GUILayout.EndArea();
                                break;
                            case "SCROLL_START":
                                scrollPos = GUILayout.BeginScrollView(scrollPos, guiStyle, GUILayout.Width(290), GUILayout.ExpandHeight(true));
                                break;
                            case "SCROLL_END":
                                GUILayout.EndScrollView();
                                break;
                            case "COURAGE":
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Courage:");
                                GUILayout.HorizontalSlider(Convert.ToSingle(val), 0, 1);
                                GUILayout.EndHorizontal();
                                break;
                            case "STUPIDITY":
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Stupidity:");
                                GUILayout.HorizontalSlider(Convert.ToSingle(val), 0, 1);
                                GUILayout.EndHorizontal();
                                break;
                            case "SANITY":
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Sanity:");
                                GUILayout.HorizontalSlider(Convert.ToSingle(val), 0, 50);
                                GUILayout.EndHorizontal();
                                break;
                        }
                    }


                    if (GUILayout.Button("Ok", buttonStyle, GUILayout.ExpandWidth(true)))//GUILayout.Button is "true" when clicked
                    {
                        if (buttonClickFunc == null)
                            Hide();
                        else
                            buttonClickFunc();
                    }
                    GUILayout.EndVertical();

                    GUI.DragWindow(new Rect(0, 0, 10000, 20));
                }
            }
        }

        public void SetTitle(string title = "")
        {
            if (HighLogic.CurrentGame.config.HasNode(_guiTextKeyUnique))
            {
                var text = HighLogic.CurrentGame.config.GetNode(_guiTextKeyUnique);
                if (text.HasNode("TITLE"))
                    text.RemoveNode("TITLE");

                if (!String.IsNullOrEmpty(title))
                {
                    var titleNode = text.AddNode("TITLE");
                    titleNode.AddValue("value", title);
                }
            }
        }

        public void ShowGuiDialog(params string[] lines)
        {
            var v = new List<KeyValuePair<string, string>>();
            v.Add(new KeyValuePair<string,string>("", "SCROLL_START"));
            foreach (string str in lines)
            {
                v.Add(new KeyValuePair<string, string>(str, "TEXT"));
            }
            v.Add(new KeyValuePair<string,string>("", "SCROLL_END"));
            ShowGuiDialog(v.ToArray());
        }

        public void ShowGuiDialog(params KeyValuePair<string, string>[] lines)
        {
            scrollPos = new Vector2();
            KFUtil.Log("ShowGuiDialog");
            if (lines.Count() > 0)
            {
                if (HighLogic.CurrentGame.config.HasNode(_guiTextKeyUnique))
                    HighLogic.CurrentGame.config.RemoveNode(_guiTextKeyUnique);

                var node = HighLogic.CurrentGame.config.AddNode(_guiTextKeyUnique);

                foreach (KeyValuePair<string, string> item in lines)
                {
                    var subnode = node.AddNode(item.Value);
                    if(!String.IsNullOrEmpty(item.Key))
                        subnode.AddValue("value", item.Key);
                }

                SetTitle();
                buttonClickFunc = null;

                KFUtil.Log("RenderingManager.AddToPostDrawQueue");
                //KFUtil.Log(node);
                RenderingManager.AddToPostDrawQueue(_queuePosition, OnDrawGUI);
            }
        }

        public bool ShowCrewDialog()
        {
            return ShowCrewDialog(FlightGlobals.ActiveVessel);
        }

        public bool ShowCrewDialog(Vessel v)
        {
            var crew = HighLogic.CurrentGame.CrewRoster.Crew;
            if (v != null) crew = v.GetVesselCrew();
            KFUtil.Log("ShowFullCrewDialog");
            List<KeyValuePair<string, string>> ButtonList = new List<KeyValuePair<string, string>>();

            ButtonList.Add(new KeyValuePair<string, string>("240", "SCROLL_START"));
            foreach (ProtoCrewMember member in crew.Where(x => KFCalc.HasFeels(x)))
            {
                ButtonList.Add(new KeyValuePair<string, string>(member.name, "BUTTON"));
            }
            ButtonList.Add(new KeyValuePair<string, string>("Any kerbals not listed are indifferent to all other kerbals.", "TEXT"));
            ButtonList.Add(new KeyValuePair<string, string>("", "SCROLL_END"));

            ShowGuiDialog(ButtonList.ToArray());

            return ButtonList.Count == 3;
        }

        public void ShowKerbalDialog(string name)
        {
            ProtoCrewMember member = HighLogic.CurrentGame.CrewRoster.Crew.First(x => x.name == name);

            if (member != null)
            {
                var FeelsList = KFCalc.GetAllFeels(member);
                var sanity = KFCalc.GetSanity(member);

                var strs = new List<KeyValuePair<string, string>>();

                strs.Add(new KeyValuePair<string, string>(sanity.ToString(), "SANITY"));
                strs.Add(new KeyValuePair<string, string>(member.courage.ToString(), "COURAGE"));
                strs.Add(new KeyValuePair<string, string>(member.stupidity.ToString(), "STUPIDITY"));

                if (FeelsList.Count > 0)
                {
                    strs.Add(new KeyValuePair<string, string>("150", "SCROLL_START"));
                    foreach (KeyValuePair<string, Feels> item in FeelsList)
                    {
                        string str;
                        switch (item.Value.Type)
                        {
                            case FeelingTypes.Indifferent:
                                str = "Indifferent";
                                if (item.Value.Number > 0) str += String.Format(" ({0})", new String('+', Convert.ToInt32(Math.Ceiling(item.Value.Number / 4))));
                                else if (item.Value.Number < 0) str += String.Format(" ({0})", new String('-', Convert.ToInt32(Math.Ceiling(Math.Abs(item.Value.Number) / 4))));
                                break;
                            case FeelingTypes.InLove:
                                str = "In Love";
                                break;
                            default:
                                str = item.Value.Type.ToString();
                                break;
                        }
                        strs.Add(new KeyValuePair<string, string>(String.Format("{0}: {1}", item.Key, str), "TEXT"));
                    }
                    strs.Add(new KeyValuePair<string, string>("", "SCROLL_END"));
                    ShowGuiDialog(strs.ToArray());
                }
                else
                    ShowGuiDialog(String.Format("{0} has no feelings towards any other kerbals.", name));

                SetTitle(_defaultTitle + ": " + name);
                buttonClickFunc = ShowCrewDialog;
            }
        }

        public void Hide()
        {
            if (HighLogic.CurrentGame.config.HasNode(_guiTextKeyUnique))
                HighLogic.CurrentGame.config.RemoveNode(_guiTextKeyUnique);

            RenderingManager.RemoveFromPostDrawQueue(_queuePosition, OnDrawGUI);
        }
    }
}
