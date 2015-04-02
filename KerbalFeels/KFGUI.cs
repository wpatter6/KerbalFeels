using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFeels
{
    class KFGUI
    {
        private bool _hidden = true;
        private const int _queuePosition = 98;
        private const string _defaultTitle = "Kerbal Feels";
        private const string _guiTextKey = "FEELS_GUI_TEXT";

        private string _guiTextKeyUnique;

        private Rect windowPosition = new Rect(150, 150, 307, 300);
        private Rect sectionPosition = new Rect(0, 0, 300, 200);

        private GUIStyle guiStyle = null;
        private GUIStyle LabelStyle = null;
        private GUIStyle scrollStyle = null;
        private GUIStyle buttonStyle = null;
        private GUIStyle subButtonStyle = null;

        private Vector2 scrollPos = new Vector2();
        private Func<bool> buttonClickFunc = null;

        public KFGUI()
        {
            KFUtil.Log("KFGUI Constructor");
            guiStyle = new GUIStyle(HighLogic.Skin.window);

            scrollStyle = new GUIStyle(guiStyle);
            scrollStyle.contentOffset = new Vector2(5, -20);
            scrollStyle.padding = new RectOffset(0, 0, 0, 0);

            buttonStyle = new GUIStyle(guiStyle);
            buttonStyle.fontSize = 20;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            subButtonStyle = new GUIStyle(guiStyle);
            subButtonStyle.fontSize = 12;
            subButtonStyle.contentOffset = new Vector2(0, -9);
            subButtonStyle.alignment = TextAnchor.MiddleLeft;
            subButtonStyle.clipping = TextClipping.Overflow;

            LabelStyle = new GUIStyle();
            LabelStyle.alignment = TextAnchor.MiddleRight;

            _guiTextKeyUnique = String.Format("{0}_{1}", _guiTextKey, Guid.NewGuid().ToString());
        }

        private void OnDrawGUI()
        {
            if (_hidden) return;
            var title = _defaultTitle;
            var text = KFConfig.GetConfigNode(HighLogic.CurrentGame.config, _guiTextKeyUnique);
            if (text.HasNode("TITLE"))
            {
                title = text.GetNode("TITLE").GetValue("value");
            }
            //Log("OnDrawGUI");
            windowPosition = GUI.Window(1, windowPosition, FeelsWindowGUI, title, guiStyle);
        }

        private void FeelsWindowGUI(int WindowID)
        {
            if (_hidden) return;
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
                            case "FEELS_LIST":
                                DoFeelsList(val);
                                break;
                            case "SECTION_START":
                                GUILayout.BeginArea(sectionPosition, val, guiStyle);
                                break;
                            case "SECTION_END":
                                GUILayout.EndArea();
                                break;
                            case "SCROLL_START":
                                scrollPos = GUILayout.BeginScrollView(scrollPos, scrollStyle, GUILayout.Width(290), GUILayout.ExpandHeight(true));
                                break;
                            case "SCROLL_END":
                                GUILayout.EndScrollView();
                                break;
                            case "COURAGE":
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Courage:");
                                GUILayout.HorizontalSlider(KFCalc.GetCourage(val), 0, 1, GUILayout.Width(150));
                                GUILayout.EndHorizontal();
                                break;
                            case "STUPIDITY":
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Stupidity:");
                                GUILayout.HorizontalSlider(KFCalc.GetStupidity(val), 0, 1, GUILayout.Width(150));
                                GUILayout.EndHorizontal();
                                break;
                            case "SANITY":
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Sanity:");

                                GUILayout.HorizontalSlider(Convert.ToSingle(KFCalc.GetSanity(val)), 0, Convert.ToSingle(KFConfig.BaseSanity), GUILayout.Width(150));
                                GUILayout.EndHorizontal();
                                break;
                        }
                    }


                    if (GUILayout.Button("Ok", buttonStyle, GUILayout.ExpandWidth(true)))
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
            KFUtil.Log("SetTitle");
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
                _hidden = false;
                RenderingManager.AddToPostDrawQueue(_queuePosition, OnDrawGUI);
            }
        }

        public bool ShowCrewDialog()
        {
            return ShowCrewDialog(FlightGlobals.ActiveVessel);
        }

        public bool ShowCrewDialog(Vessel v)
        {
            KFUtil.Log("ShowFullCrewDialog");

            var crew = HighLogic.CurrentGame.CrewRoster.Crew;
            if (v != null) crew = v.GetVesselCrew();
            List<KeyValuePair<string, string>> ButtonList = new List<KeyValuePair<string, string>>();

            ButtonList.Add(new KeyValuePair<string, string>("240", "SCROLL_START"));
            foreach (ProtoCrewMember member in crew)
            {
                ButtonList.Add(new KeyValuePair<string, string>(member.name, "BUTTON"));
            }
            ButtonList.Add(new KeyValuePair<string, string>("", "SCROLL_END"));

            ShowGuiDialog(ButtonList.ToArray());

            if (v != null)
                SetTitle(_defaultTitle + ": " + v.vesselName);

            return ButtonList.Count == 3;
        }

        private void DoFeelsList(string name)
        {
            KFUtil.Log("DoFeelsList");
            ProtoCrewMember member = HighLogic.CurrentGame.CrewRoster.Crew.First(x => x.name == name);
            if (member != null)
            {
                var FeelsList = KFCalc.GetAllFeels(member);
                scrollPos = GUILayout.BeginScrollView(scrollPos, scrollStyle, GUILayout.Width(290), GUILayout.ExpandHeight(true));

                GUILayout.Label(KFUtil.GetFirstName(name) + " is");
                if (FeelsList.Count > 0)
                {
                    foreach (KeyValuePair<string, Feels> item in FeelsList)
                    {
                        string str = KFUtil.GetFeelsString(item.Value);

                        GUILayout.Label(String.Format("{1} {0}", item.Key, str));
                    }
                }
                else GUILayout.Label("indifferent towards everybody.");
                GUILayout.EndScrollView();
            }
        }

        public void ShowKerbalDialog(string name)
        {
            KFUtil.Log("ShowKerbalDialog");
            ProtoCrewMember member = HighLogic.CurrentGame.CrewRoster.Crew.First(x => x.name == name);

            if (member != null)
            {
                var FeelsList = KFCalc.GetAllFeels(member);
                var sanity = KFCalc.GetSanity(member);

                var strs = new List<KeyValuePair<string, string>>();

                strs.Add(new KeyValuePair<string, string>(member.name, "SANITY"));
                strs.Add(new KeyValuePair<string, string>(member.name, "COURAGE"));
                strs.Add(new KeyValuePair<string, string>(member.name, "STUPIDITY"));
                strs.Add(new KeyValuePair<string, string>(member.name, "FEELS_LIST"));
                /*if (FeelsList.Count > 0)
                {
                    strs.Add(new KeyValuePair<string, string>("150", "SCROLL_START"));
                    foreach (KeyValuePair<string, Feels> item in FeelsList)
                    {
                        string str = KFUtil.GetFeelsString(item.Value);
                        
                        strs.Add(new KeyValuePair<string, string>(String.Format("{0}: {1}", item.Key, str), "TEXT"));
                    }
                    strs.Add(new KeyValuePair<string, string>("", "SCROLL_END"));
                }
                else
                    strs.Add(new KeyValuePair<string, string>(String.Format("{0} has no feelings towards any other kerbals.", name), "TEXT"));
                */
                ShowGuiDialog(strs.ToArray());

                SetTitle(_defaultTitle + ": " + name);
                buttonClickFunc = ShowCrewDialog;
            }
        }

        public void Hide()
        {
            KFUtil.Log("Hide");
            _hidden = true;
            RenderingManager.RemoveFromPostDrawQueue(_queuePosition, OnDrawGUI);
            KFConfig.AppButton.SetFalse(false);
            if (HighLogic.CurrentGame.config.HasNode(_guiTextKeyUnique))
                HighLogic.CurrentGame.config.RemoveNode(_guiTextKeyUnique);

        }
    }
}
