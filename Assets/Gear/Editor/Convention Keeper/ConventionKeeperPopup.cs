using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Gear.Tools.ConventionKeeper
{
    #region Data Types

    /// <summary>
    /// Custom data for dialog buttons.
    /// </summary>
    public class ButtonData
    {
        /// <summary>
        /// Holds button label.
        /// </summary>
        public string label;

        /// <summary>
        /// Holds the button callback.
        /// </summary>
        public Action callback;

        /// <summary>
        /// Holds input callback.
        /// </summary>
        public Action<string> inputCallback;

        /// <summary>
        /// Setup ButtonData for a simple button.
        /// </summary>
        /// <param name="_label">Button label.</param>
        /// <param name="_callback">Button callback</param>
        public ButtonData(string _label, Action _callback)
        {
            this.label = _label;
            this.callback = _callback;
        }

        /// <summary>
        /// Setup ButtonData for a input related button.
        /// </summary>
        /// <param name="_label">Button label.</param>
        /// <param name="_callback">Input callback</param>
        public ButtonData(string _label, Action<string> _inputCallback)
        {
            this.label = _label;
            this.inputCallback = _inputCallback;
        }

        /// <summary>
        /// Calls the simple button callback.
        /// </summary>
        public void Invoke()
        {
            if (callback != null)
            {
                this.callback.Invoke();
            }
        }

        /// <summary>
        /// Calls the input callback.
        /// </summary>
        /// <param name="inputText">The text to be used by the callback.</param>
        public void Invoke(string inputText)
        {
            if (inputCallback != null)
            {
                this.inputCallback.Invoke(inputText);
            }
        }
    }

    /// <summary>
    /// Tab options for the Overview Window
    /// </summary>
    public enum TabOption
    {
        Folders = 0,
        Files,
        Configs,
        NumberOfTabs
    }

    #endregion

    /// <summary>
    /// General class for dialog management.
    /// </summary>
    public class ConventionKeeperPopup : EditorWindow
    {
        #region Public Variables

        /// <summary>
        /// The window.
        /// </summary>
        ConventionKeeperPopup window;

        /// <summary>
        /// Holds dialog title.
        /// </summary>
        public string title = string.Empty;

        /// <summary>
        /// Holds dialog message.
        /// </summary>
        public string message = string.Empty;

        /// <summary>
        /// Holds button list.
        /// </summary>
        public List<ButtonData> buttonList = new List<ButtonData>();

        /// <summary>
        /// Holds the draw function of a given dialog.
        /// </summary>
        public Action drawFunction = null;

        /// <summary>
        /// Holds the input field text.
        /// </summary>
        public string inputFieldText = string.Empty;

        /// <summary>
        /// Holds the input field original text.
        /// </summary>
        public string inputFieldFirstText = string.Empty;

        /// <summary>
        /// Holds the current tab of the Overview Window
        /// </summary>
        public TabOption tab;

        /// <summary>
        /// Holds the scroll position.
        /// </summary>
        Vector2 scrollPos;

        /// <summary>
        /// Holds the configuration text for editing on Overview Window.
        /// </summary>
        public string configText;

        Color backgroundColor = new Color(0.5f, 0.9f, 0.9f, 0.25f);

        #endregion

        #region Dialog Setups

        /// <summary>
        /// Setup for the 2 input buttons dialog.
        /// </summary>
        public static void Dialog(string title, string message, ButtonData ok, ButtonData cancel, int width = 0, int height = 0, Color customBGColor = new Color())
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

            window.backgroundColor = customBGColor;

            window.ShowPopup();
        }

        /// <summary>
        /// Setup for the N input buttons dialog.
        /// </summary>
        public static void Dialog(string title, string message, List<ButtonData> buttonList, int width = 0, int height = 0, Color customBGColor = new Color())
        {
            ConventionKeeperPopup window = ScriptableObject.CreateInstance<ConventionKeeperPopup>();

            width = (width == 0) ? Mathf.RoundToInt(Screen.currentResolution.width * 0.5f) : width;
            height = (height == 0) ? Mathf.RoundToInt(Screen.currentResolution.height * 0.5f) : height;

            window.position = new Rect(Screen.currentResolution.width / 2 - width / 2, Screen.currentResolution.height / 2 - height / 2, width, height);

            window.title = title;
            window.message = message;

            window.buttonList = buttonList;

            window.drawFunction = new Action(window.DrawDialog);

            window.backgroundColor = customBGColor;

            window.ShowPopup();
        }

        /// <summary>
        /// Setup for the 2 input buttons and 1 input field dialog.
        /// </summary>
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

        /// <summary>
        /// Setup for the First Time Stup dialog.
        /// </summary>
        public static void FirstTimeDialog(int width, int height)
        {
            ConventionKeeperPopup window = ScriptableObject.CreateInstance<ConventionKeeperPopup>();

            width = (width == 0) ? Mathf.RoundToInt(Screen.currentResolution.width * 0.5f) : width;
            height = (height == 0) ? Mathf.RoundToInt(Screen.currentResolution.height * 0.5f) : height;

            window.position = new Rect(Screen.currentResolution.width / 2 - width / 2, Screen.currentResolution.height / 2 - height / 2, width, height);

            window.title = "Hello!";
            window.message = "We need to setup Convention Keeper to support your project convention needs!\n" +
                             "\n" +
                             "What do you want to do?";

            window.drawFunction = new Action(window.DrawFirstTimeDialog);

            window.ShowPopup();
        }

        /// <summary>
        /// Setup for the Overview Window.
        /// </summary>
        public static void OverviewDialog()
        {
            int width = Mathf.RoundToInt(Screen.currentResolution.width * 0.5f);
            int height = 500;

            ConventionKeeperPopup window = ScriptableObject.CreateInstance<ConventionKeeperPopup>();
            window.titleContent.text = ConventionKeeper.toolName + " " + ConventionKeeper.toolVersion;

            width = (width == 0) ? Mathf.RoundToInt(Screen.currentResolution.width * 0.5f) : width;
            height = (height == 0) ? Mathf.RoundToInt(Screen.currentResolution.height * 0.5f) : height;

            window.position = new Rect(Screen.currentResolution.width / 2 - width / 2, Screen.currentResolution.height / 2 - height / 2, width, height);

            window.title = "Overview";
            window.message = "We need to setup Convention Keeper to support your project needs!";

            window.drawFunction = new Action(window.DrawOverviewDialog);

            window.Show();
        }

        #endregion

        #region Dialog Draw Functions

        /// <summary>
        /// Simple dialog draw function.
        /// </summary>
        public void DrawDialog()
        {
            EditorGUI.DrawRect(new Rect(0, 0, maxSize.x, maxSize.y), backgroundColor);

            if (GUILayout.Button("X"))
            {
                this.Close();
            }

            GUILayout.Label(this.title);

            GUILayout.BeginScrollView(Vector2.zero);
            {
                GUILayout.Label(this.message);
            }
            GUILayout.EndScrollView();

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

        /// <summary>
        /// Dialog with input field draw function.
        /// </summary>
        public void DrawDialogWithInputField()
        {
            EditorGUI.DrawRect(new Rect(0, 0, maxSize.x, maxSize.y), backgroundColor);

            if (GUILayout.Button("X"))
            {
                this.Close();
            }

            GUILayout.Label(this.title);

            GUILayout.BeginScrollView(Vector2.zero);
            {
                GUILayout.Label(this.message);
            }
            GUILayout.EndScrollView();

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

                    DrawButton(buttonList[i].label, delegate ()
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
                    });
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// First Time Setup draw function.
        /// </summary>
        public void DrawFirstTimeDialog()
        {
            EditorGUI.DrawRect(new Rect(0, 0, maxSize.x, maxSize.y), backgroundColor);

            DrawButton("X", delegate ()
            {
                EditorPrefs.SetBool(ConventionKeeper.setupDoneKey, false);
                this.Close();
            });

            GUILayout.Label(this.title);

            GUILayout.BeginScrollView(Vector2.zero);
            {
                GUILayout.Label(this.message);
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            {
                DrawButton("START!", delegate ()
                {
                    this.Close();
                    ConventionKeeper.SetupFirstTime();
                });

                DrawButton("Nothing, thanks.", delegate ()
                {
                    EditorPrefs.SetBool(ConventionKeeper.setupDoneKey, false);
                    this.Close();
                });
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw button function.
        /// </summary>
        /// <param name="label">Button label.</param>
        /// <param name="callback">Button callback.</param>
        /// <param name="width">(Optional) Button width.</param>
        public void DrawButton(string label, Action callback, int width = -1)
        {
            width = (width == 0) ? label.Length : width;

            if (width == -1)
            {
                if (GUILayout.Button(label))
                {
                    callback.Invoke();
                }
            }
            else
            {
                width *= 10;

                if (GUILayout.Button(label, GUILayout.Width(width)))
                {
                    callback.Invoke();
                }
            }
        }

        /// <summary>
        /// Overview Window draw function.
        /// </summary>
        public void DrawOverviewDialog()
        {
            EditorGUI.DrawRect(new Rect(0, 0, maxSize.x, maxSize.y), backgroundColor);

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;

            GUILayout.Label("<b><color=maroon>Attention: </color>\n 1) Fix as much folders as possible.\n 2) Fix remaining files.\n 3) Edit Configs to deal with remaining folder/files.</b>", style);

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

                    GUILayout.Label("Not Convention Valid Folders: " + ConventionKeeper.overviewFolderList.Count);

                    if (ConventionKeeper.overviewFolderList.Count == 0)
                    {
                        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);
                        {
                            GUILayout.Label("<color=green><b>No folder convention problems, good!</b></color>", style);
                        }
                        GUILayout.EndScrollView();
                        break;
                    }

                    scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);
                    {
                        bool breakLoop = false;

                        foreach (var item in ConventionKeeper.overviewFolderList)
                        {
                            string errors = "<color=red>";

                            foreach (FolderConventionState error in item.folderError)
                            {
                                errors += " | " + error.ToString();
                            }

                            errors += "</color>";

                            GUILayout.BeginHorizontal();
                            {
                                DrawButton("FIX IT!", delegate ()
                                {
                                    breakLoop = true;

                                    //Fix NotValid state first because it means the convention system is not tracking it.
                                    if (item.folderError.Contains(FolderConventionState.NotValid))
                                    {
                                        //Fix it
                                        ConventionKeeper.AddPathToConvention(item.assetsFullPath);
                                    }

                                    //Fix noFoldersAllowed second because with no folders allowance the code does not check child folders and just halt.
                                    if (item.folderError.Contains(FolderConventionState.NoFoldersAllowed))
                                    {
                                        //Fix it
                                        ConventionKeeper.AddFolderTypeToPath(item.assetsFullPath);
                                    }

                                    //Reset Overview Data - Recheck convention
                                    RefreshOverviewData();
                                }, 10);

                                DrawButton("Ignore", delegate ()
                                {
                                    ConventionKeeper.AddToIgnoredPaths(item.assetsFullPath);

                                    //Reset Overview Data - Recheck convention
                                    RefreshOverviewData();
                                }, 10);

                                DrawButton("Delete", delegate ()
                                {
                                    ConventionKeeper.DeleteFolder(item.assetsFullPath);

                                    //Reset Overview Data - Recheck convention
                                    RefreshOverviewData();
                                }, 10);

                                GUILayout.Label(item.assetsFullPath + errors, style);
                            }
                            GUILayout.EndHorizontal();

                            if (breakLoop)
                            {
                                break;
                            }
                        }
                    }
                    GUILayout.EndScrollView();

                    break;
                case TabOption.Files:

                    GUILayout.Label("Not Convention Valid Files: " + ConventionKeeper.overviewFileList.Count);

                    if (ConventionKeeper.overviewFolderList.Count > 0)
                    {
                        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);
                        {
                            GUILayout.Label("<color=maroon>You need to resolve all the <b>folder issues first!</b></color>", style);
                        }
                        GUILayout.EndScrollView();
                        break;
                    }

                    if (ConventionKeeper.overviewFileList.Count == 0)
                    {
                        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);
                        {
                            GUILayout.Label("<color=green><b>No file convention problems, good!</b></color>", style);
                        }
                        GUILayout.EndScrollView();
                        break;
                    }

                    scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);
                    {
                        bool breakLoop = false;

                        foreach (var item in ConventionKeeper.overviewFileList)
                        {
                            string errors = "<color=red>";

                            foreach (var error in item.fileError)
                            {
                                errors += " | " + error.ToString();

                                if (error == FileConventionState.NotValid)
                                {
                                    errors += " - <b>Is the file in the right folder?</b>";
                                }
                            }

                            errors += "</color>";
                            GUILayout.BeginHorizontal();
                            {
                                if (item.fileError.Count > 0 && !item.fileError.Contains(FileConventionState.NotValid))
                                {
                                    DrawButton("FIX IT!", delegate ()
                                    {
                                        breakLoop = true;

                                        //First fix file name as it may affect the file type(?)
                                        if (item.fileError.Contains(FileConventionState.WrongFileName))
                                        {
                                            ConventionKeeper.FileNameChangeDialog(item);
                                        }

                                        //Fix unknown file type - This just fix already stablished name conventions/file types in the config doc!
                                        if (item.fileError.Contains(FileConventionState.UnknownFileType))
                                        {
                                            ConventionKeeper.AddFileTypeToPath(item);
                                        }

                                        //Reset Overview Data - Recheck convention
                                        RefreshOverviewData();
                                    }, 10);
                                }

                                if (ConventionKeeper.IsValidFileType(item.type))
                                {
                                    DrawButton("Ignore File", delegate ()
                                    {
                                        breakLoop = true;

                                        ConventionKeeper.AddToIgnoredFiles(item);

                                        //Reset Overview Data - Recheck convention
                                        RefreshOverviewData();
                                    }, 10);
                                }

                                if (ConventionKeeper.IsValidFileType(item.type))
                                {
                                    DrawButton("Ignore Type", delegate ()
                                    {
                                        breakLoop = true;

                                        ConventionKeeper.AddToIgnoredFileTypes(item);

                                        //Reset Overview Data - Recheck convention
                                        RefreshOverviewData();
                                    }, 10);
                                }

                                if (item.type != "cs")
                                {
                                    DrawButton("Delete File", delegate ()
                                    {
                                        breakLoop = true;

                                        ConventionKeeper.DeleteDialog(item);

                                    //Reset Overview Data - Recheck convention
                                    RefreshOverviewData();
                                    }, 10);
                                }

                                GUILayout.Label(item.assetsFullPath + errors, style);
                            }
                            GUILayout.EndHorizontal();

                            if (breakLoop)
                            {
                                break;
                            }
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

                    if (configText != ConventionKeeper.config.ToString(true))
                    {
                        GUI.enabled = true;
                    }
                    else
                    {
                        GUI.enabled = false;
                    }

                    if (GUILayout.Button("Save Configuration File"))
                    {
                        ConventionKeeper.config = new JSONObject(configText);

                        ConventionKeeper.SaveConfigs();
                    }

                    GUI.enabled = true;

                    break;
            }

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Refresh"))
                {
                    RefreshOverviewData();
                }

                if (!EditorPrefs.HasKey(ConventionKeeper.setupDoneKey))
                {
                    if (GUILayout.Button("Cancel"))
                    {
                        ConventionKeeper.usingOverview = false;
                        EditorPrefs.SetBool(ConventionKeeper.setupDoneKey, false);
                        this.Close();
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                if (!EditorPrefs.HasKey(ConventionKeeper.setupDoneKey) || !EditorPrefs.GetBool(ConventionKeeper.setupDoneKey))
                {
                    if (ConventionKeeper.overviewFolderList.Count > 0 || ConventionKeeper.overviewFileList.Count > 0)
                    {
                        GUI.enabled = false;
                    }
                    else
                    {
                        GUI.enabled = true;
                    }

                    if (GUILayout.Button("Finish First Time Setup"))
                    {
                        EditorPrefs.SetBool(ConventionKeeper.setupDoneKey, true);
                        this.Close();
                    }

                    GUI.enabled = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        public void RefreshOverviewData()
        {
            configText = ConventionKeeper.config.ToString(true);
            ConventionKeeper.RefreshOverview();
        }

        #endregion

        #region Unity Callbacks

        /// <summary>
        /// Unity window draw function.
        /// </summary>
        private void OnGUI()
        {
            //If there is a draw function, call it
            if (this.drawFunction != null)
            {
                this.drawFunction.Invoke();
            }
            else //If not, close the window
            {
                this.Close();
            }
        }

        /// <summary>
        /// Unity window disable callback.
        /// </summary>
        private void OnDisable()
        {
            ConventionKeeper.usingOverview = false;
        }

        #endregion
    }
}
