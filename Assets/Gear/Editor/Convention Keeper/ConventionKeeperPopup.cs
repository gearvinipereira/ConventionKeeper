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

        public ButtonData(string _label, Action _callback)
        {
            this.label = _label;
            this.callback = _callback;
        }

        public void Invoke()
        {
            if (callback != null)
            {
                this.callback.Invoke();
            }
        }
    }

    public class ConventionKeeperPopup : EditorWindow
    {
        public string title = string.Empty;
        public string message = string.Empty;
        public List<ButtonData> buttonList = new List<ButtonData>();

        public static void Open(string title, string message, ButtonData ok, int width, int height)
        {
            ConventionKeeperPopup window = ScriptableObject.CreateInstance<ConventionKeeperPopup>();
            window.position = new Rect(Screen.currentResolution.width / 2, Screen.currentResolution.height / 2, width, height);

            window.title = title;
            window.message = message;

            window.buttonList = new List<ButtonData>();
            window.buttonList.Add(ok);

            window.ShowPopup();
        }

        public static void Open(string title, string message, ButtonData ok, ButtonData cancel, int width, int height)
        {
            ConventionKeeperPopup window = ScriptableObject.CreateInstance<ConventionKeeperPopup>();
            window.position = new Rect(Screen.currentResolution.width / 2 - width / 2, Screen.currentResolution.height / 2 - height / 2, width, height);

            window.title = title;
            window.message = message;

            window.buttonList = new List<ButtonData>();
            window.buttonList.Add(ok);
            window.buttonList.Add(cancel);
            
            window.ShowPopup();
        }

        private void OnGUI()
        {
            /*if (GUILayout.Button("X"))
            {
                this.Close();
            }*/

            GUILayout.Label(this.title);

            GUILayout.Label(this.message);

            foreach (ButtonData button in this.buttonList)
            {
                if (GUILayout.Button(button.label))
                {
                    button.Invoke();
                    this.Close();
                }
            }
        }
    }
}
