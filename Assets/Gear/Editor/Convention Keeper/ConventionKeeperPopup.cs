using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Gear.Tools.ConventionKeeper
{

    public class ButtonData
    {
        public string label;
        public Action callback;
        public Action<string> inputCallback;

        public ButtonData(string _label, Action _callback)
        {
            this.label = _label;
            this.callback = _callback;
        }

        public ButtonData(string _label, Action<string> _inputCallback)
        {
            this.label = _label;
            this.inputCallback = _inputCallback;
        }

        public void Invoke()
        {
            if (callback != null)
            {
                this.callback.Invoke();
            }
        }

        public void Invoke(string inputText)
        {
            if (inputCallback != null)
            {
                this.inputCallback.Invoke(inputText);
            }
        }
    }

    public enum TabOption
    {
        Folders = 0,
        Files,
        Configs,
        NumberOfTabs
    }

    public class ConventionKeeperPopup : EditorWindow
    {
        public string title = string.Empty;
        public string message = string.Empty;
        public List<ButtonData> buttonList = new List<ButtonData>();
        public Action drawFunction = null;
        public string inputFieldText = string.Empty;
        public string inputFieldFirstText = string.Empty;
        public TabOption tab;
        Vector2 scrollPos;
        public string configText;

        public static void Dialog(string title, string message, ButtonData ok, int width = 0, int height = 0)
        {
            ConventionKeeperPopup window = ScriptableObject.CreateInstance<ConventionKeeperPopup>();

            width = (width == 0) ? Mathf.RoundToInt(Screen.currentResolution.width * 0.5f) : width;
            height = (height == 0) ? Mathf.RoundToInt(Screen.currentResolution.height * 0.5f) : height;

            window.position = new Rect(Screen.currentResolution.width / 2, Screen.currentResolution.height / 2, width, height);

            window.title = title;
            window.message = message;

            window.buttonList = new List<ButtonData>();
            window.buttonList.Add(ok);

            window.drawFunction = new Action(window.DrawDialog);

            window.ShowPopup();
        }

        public static void Dialog(string title, string message, ButtonData ok, ButtonData cancel, int width = 0, int height = 0)
        {
            ConventionKeeperPopup window = ScriptableObject.CreateInstance<ConventionKeeperPopup>();

            width = (width == 0) ? Mathf.RoundToInt(Screen.currentResolution.width * 0.5f) : width;
            height = (height == 0) ? Mathf.RoundToInt(Screen.currentResolution.height * 0.5f) : height;

            window.position = new Rect(Screen.currentResolution.width / 2 - width / 2, Screen.currentResolution.height / 2 - height / 2, width, height);

            window.title = title;
            window.message = message;

            window.buttonList = new List<ButtonData>();
            window.buttonList.Add(ok);
            window.buttonList.Add(cancel);

            window.drawFunction = new Action(window.DrawDialog);

            window.ShowPopup();
        }

        public static void Dialog(string title, string message, List<ButtonData> buttonList, int width = 0, int height = 0)
        {
            ConventionKeeperPopup window = ScriptableObject.CreateInstance<ConventionKeeperPopup>();

            width = (width == 0) ? Mathf.RoundToInt(Screen.currentResolution.width * 0.5f) : width;
            height = (height == 0) ? Mathf.RoundToInt(Screen.currentResolution.height * 0.5f) : height;

            window.position = new Rect(Screen.currentResolution.width / 2 - width / 2, Screen.currentResolution.height / 2 - height / 2, width, height);

            window.title = title;
            window.message = message;

            window.buttonList = buttonList;

            window.drawFunction = new Action(window.DrawDialog);

            window.ShowPopup();
        }

        public static void DialogWithInputField(string title, string message, string inputInitialText, ButtonData ok, ButtonData cancel, int width = 0, int height = 0)
        {
            ConventionKeeperPopup window = ScriptableObject.CreateInstance<ConventionKeeperPopup>();

            width = (width == 0) ? Mathf.RoundToInt(Screen.currentResolution.width * 0.5f) : width;
            height = (height == 0) ? Mathf.RoundToInt(Screen.currentResolution.height * 0.5f) : height;

            window.position = new Rect(Screen.currentResolution.width / 2 - width / 2, Screen.currentResolution.height / 2 - height / 2, width, height);

            window.title = title;
            window.message = message;

            window.buttonList = new List<ButtonData>();
            window.buttonList.Add(ok);
            window.buttonList.Add(cancel);

            window.inputFieldFirstText = inputInitialText;
            window.inputFieldText = inputInitialText;

            window.drawFunction = new Action(window.DrawDialogWithInputField);

            window.ShowPopup();
        }

        public static void ConfigEditDialog()
        {

        }

        public static void FirstTimeDialog(int width, int height)
        {
            ConventionKeeperPopup window = ScriptableObject.CreateInstance<ConventionKeeperPopup>();

            width = (width == 0) ? Mathf.RoundToInt(Screen.currentResolution.width * 0.5f) : width;
            height = (height == 0) ? Mathf.RoundToInt(Screen.currentResolution.height * 0.5f) : height;

            window.position = new Rect(Screen.currentResolution.width / 2 - width / 2, Screen.currentResolution.height / 2 - height / 2, width, height);

            window.title = "Hello!";
            window.message = "We need to setup Convention Keeper to support your project needs!" +
                             "\n" +
                             "\nThe tool will make a series of questions regarding your project folders, answer wisely!";



            window.drawFunction = new Action(window.DrawFirstTimeDialog);

            window.ShowPopup();
        }

        public static void OverviewDialog()
        {
            int width = Mathf.RoundToInt(Screen.currentResolution.width * 0.75f);
            int height = 500;

            ConventionKeeperPopup window = ScriptableObject.CreateInstance<ConventionKeeperPopup>();

            width = (width == 0) ? Mathf.RoundToInt(Screen.currentResolution.width * 0.5f) : width;
            height = (height == 0) ? Mathf.RoundToInt(Screen.currentResolution.height * 0.5f) : height;

            window.position = new Rect(Screen.currentResolution.width / 2 - width / 2, Screen.currentResolution.height / 2 - height / 2, width, height);

            window.title = "Overview";
            window.message = "We need to setup Convention Keeper to support your project needs!";

            window.drawFunction = new Action(window.DrawOverviewDialog);

            window.ShowPopup();
        }

        private void OnGUI()
        {
            if (this.drawFunction != null)
            {
                this.drawFunction.Invoke();
            }
            else
            {
                this.Close();
            }
        }

        public void DrawDialog()
        {
            if (GUILayout.Button("X"))
            {
                this.Close();
            }

            GUILayout.Label(this.title);

            GUILayout.Label(this.message);

            GUILayout.BeginHorizontal();
            {
                foreach (ButtonData button in this.buttonList)
                {
                    if (GUILayout.Button(button.label))
                    {
                        button.Invoke();
                        this.Close();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        public void DrawDialogWithInputField()
        {
            if (GUILayout.Button("X"))
            {
                this.Close();
            }

            GUILayout.Label(this.title);

            GUILayout.Label(this.message);

            this.inputFieldText = GUILayout.TextField(inputFieldText);

            GUILayout.BeginHorizontal();
            {
                //foreach (ButtonData button in this.buttonList)
                for (int i = 0; i < this.buttonList.Count; i++)
                {
                    if (i == 0 && this.inputFieldText == this.inputFieldFirstText)
                    {
                        GUI.enabled = false;
                    }
                    else
                    {
                        GUI.enabled = true;
                    }

                    if (GUILayout.Button(buttonList[i].label))
                    {
                        if (i == 0)
                        {
                            buttonList[i].Invoke(this.inputFieldText);
                        }
                        else
                        {
                            buttonList[i].Invoke();
                        }
                        this.Close();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        public void DrawFirstTimeDialog()
        {
            if (GUILayout.Button("X"))
            {
                EditorPrefs.SetBool(ConventionKeeper.setupDoneKey, false);
                this.Close();
            }

            GUILayout.Label(this.title);

            GUILayout.Label(this.message);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("START!"))
                {
                    this.Close();
                    ConventionKeeper.SetupFirstTime();
                }

                if (GUILayout.Button("No thanks."))
                {
                    EditorPrefs.SetBool(ConventionKeeper.setupDoneKey, false);
                    this.Close();
                }

                
            }
            GUILayout.EndHorizontal();
        }

        public void DrawOverviewDialog()
        {
            if (GUILayout.Button("X"))
            {
                this.Close();
            }

            string[] tabs = new string[(int)TabOption.NumberOfTabs];

            for (int i = 0; i < (int)TabOption.NumberOfTabs; i++)
            {
                tabs[i] = ((TabOption)i).ToString();
            }

            tab = (TabOption)GUILayout.Toolbar((int)tab, tabs);

            if (configText == null)
            {
                configText = ConventionKeeper.config.ToString(true);
            }

            switch (tab)
            {
                case TabOption.Folders:

                    scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);
                    {
                        foreach (var item in ConventionKeeper.overviewFolderList)
                        {
                            string errors = "";

                            foreach (var error in item.folderError)
                            {
                                errors += " | " + error.ToString();
                            }

                            GUILayout.Label(item.assetsFullPath + errors);
                        }
                    }
                    GUILayout.EndScrollView();

                    break;
                case TabOption.Files:

                    scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);
                    {
                        foreach (var item in ConventionKeeper.overviewFileList)
                        {
                            string errors = "";

                            foreach (var error in item.folderError)
                            {
                                errors += " | " + error.ToString();
                            }

                            GUILayout.Label(item.assetsFullPath + errors);
                        }
                    }
                    GUILayout.EndScrollView();

                    break;
                case TabOption.Configs:
                    scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);
                    {
                        configText = GUILayout.TextArea(configText);
                    }
                    GUILayout.EndScrollView();
                    break;
            }

            if (GUILayout.Button("Refresh"))
            {
                ConventionKeeper.ClearOverview();
                ConventionKeeper.OverviewProcessFolder("Assets");
            }

            if (!EditorPrefs.HasKey(ConventionKeeper.setupDoneKey))
            {
                if (GUILayout.Button("Cancel"))
                {
                    EditorPrefs.SetBool(ConventionKeeper.setupDoneKey, false);
                    this.Close();
                }
            }
        }
    }
}
