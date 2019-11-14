/* Convention Keeper Tool - v0.2 - By Vinicius Pereira - Gear Inc.
 * 
 * == TODO List ==
 *      = Add possibility to ignore file types per folder too.
 * 
 * 
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using System;

namespace Gear.Tools.ConventionKeeper
{

    #region ENUMS

    /// <summary>
    /// Defines the folder state
    /// </summary>
    public enum FolderConventionState
    {
        Valid,
        Ignored,
        UnknownFiles,
        NoFoldersAllowed,
        NotValid
    }

    /// <summary>
    /// Difnes the file state
    /// </summary>
    public enum FileConventionState
    {
        Valid,
        Ignored,
        WrongDirectory,
        WrongFileName,
        UnknownFileType,
        NotValid
    }

    #endregion

    /// <summary>
    /// Defines a basic file data class for easy of use
    /// </summary>
    public class FileData
    {
        public string name;
        public string fullName;
        public string type;
        public string assetsFullPath;
        public string folderAssetsPath;
        public List<FolderConventionState> folderError = new List<FolderConventionState>();
        public List<FileConventionState> fileError = new List<FileConventionState>();

        public FileData(string fullFilePath)
        {
            name = Path.GetFileNameWithoutExtension(fullFilePath);

            string extension = Path.GetExtension(fullFilePath);
            type = (extension != string.Empty) ? extension.Remove(0, 1) : "folder";
            type = type.ToLower();

            fullName = name + "." + type;

            assetsFullPath = fullFilePath.Remove(0, fullFilePath.IndexOf("Assets"));

            if (fullFilePath.Contains("\\"))
            {
                assetsFullPath = assetsFullPath.Replace("\\", "/");
            }

            if (fullFilePath.Contains("/"))
            {
                folderAssetsPath = assetsFullPath.Remove(assetsFullPath.IndexOf("/" + name));
            }
        }
    }

    [InitializeOnLoad]
    public class ConventionKeeper : MonoBehaviour
    {
        /// <summary>
        /// Holds the path to the configuration JSON file
        /// </summary>
        private const string configFilePath = "Assets/Gear/Config Files/ConventionKeeperConfig.json";

        private const string baseConfigFilePath = "Assets/Gear/Config Files/ConventionKeeperConfigBase.json";

        private static string projectConfigFilePath = "Assets/Gear/Config Files/ConventionKeeper_{NAME}_Config.asset";

        private const string autoRecheckKey = "AutoRecheckAfterDialog";

        public const string setupDoneKey = "isSetupDone";

        private const string toolName = "Convention Keeper";

        private const string toolVersion = "v0.2";

        /// <summary>
        /// Holds the configuration JSON file data
        /// </summary>
        public static JSONObject config = null;

        /// <summary>
        /// Holds the folder errors message
        /// </summary>
        private static string folderErrors;

        /// <summary>
        /// Holds the state of the current tool, it might be deactivated
        /// </summary>
        public static bool active = false;

        //Overview Vars
        public static List<FileData> overviewFolderList = new List<FileData>();
        public static List<FileData> overviewFileList = new List<FileData>();

        /// <summary>
        /// Function called once when unity opens
        /// </summary>
        static ConventionKeeper()
        {
            if (!ConventionKeeper.GetConfigFileExists() && config == null)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initializes the Convention Keeper Tool
        /// </summary>
        [MenuItem("Gear/Convention Checker/Check Convention", priority = 0)]
        public static void Initialize()
        {
            //Setup config path with app name
            projectConfigFilePath = projectConfigFilePath.Replace("{NAME}", Application.productName);

            if (EditorPrefs.HasKey(setupDoneKey))
            {
                if (EditorPrefs.GetBool(setupDoneKey)) //If the setup was done the Convention Keeper runs, if not it assumes its not to.
                {
                    RunConventionCheck();
                }
            }
            else
            {
                ConventionKeeperPopup.FirstTimeDialog(500, 250);
            }
        }

        [MenuItem("Gear/Convention Checker/Fake Auto Start")]
        public static void FakeAutoStart()
        {
            if (config != null)
            {
                Clear();
                config = null;
            }

            Initialize();
        }

        [MenuItem("Gear/Convention Checker/Fake Fresh Start")]
        public static void FakeFreshStart()
        {
            if (config != null)
            {
                Clear();
                config = null;
            }

            if (EditorPrefs.HasKey(autoRecheckKey))
            {
                EditorPrefs.DeleteKey(autoRecheckKey);
            }

            if (EditorPrefs.HasKey(setupDoneKey))
            {
                EditorPrefs.DeleteKey(setupDoneKey);
            }

            AssetDatabase.DeleteAsset(projectConfigFilePath);

            Initialize();
        }

        public static void SetupFirstTime()
        {
            TextAsset configTextAsset;

            UnityEngine.Object configFileData = AssetDatabase.LoadAssetAtPath(baseConfigFilePath, typeof(UnityEngine.Object));

            configTextAsset = new TextAsset(configFileData.ToString());

            AssetDatabase.CreateAsset(configTextAsset, projectConfigFilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadConfigs();

            OverviewProcessFolder("Assets");

            ConventionKeeperPopup.OverviewDialog();

            //Initialize();
        }

        public static void Clear()
        {
            config.Clear();
            config = null;
            active = false;
        }

        /// <summary>
        /// Loads the configuration JSON data into respective variables
        /// </summary>
        [MenuItem("Gear/Convention Checker/Reload Configs")]
        public static void LoadConfigs()
        {
            if (config != null)
            {
                Clear();
            }

            TextAsset configFileData = (TextAsset)AssetDatabase.LoadAssetAtPath(projectConfigFilePath, typeof(TextAsset));

            config = new JSONObject(configFileData.text);

            active = config["active"].b;

            if (!active)
            {
                config.Clear();
                return;
            }
        }

        [MenuItem("Gear / Convention Checker / Open Overview", priority = 1)]
        public static void OpenOverview()
        {
            if (EditorPrefs.HasKey(setupDoneKey))
            {
                ClearOverview();

                ConventionKeeper.OverviewProcessFolder("Assets");

                ConventionKeeperPopup.OverviewDialog();
            }
        }

        public static bool GetConfigFileExists()
        {
            string path = Application.dataPath.Remove(Application.dataPath.IndexOf("Assets")) + projectConfigFilePath;
            return File.Exists(path);
        }

        public static void SaveConfigs()
        {
            TextAsset configObject = new TextAsset(config.ToString(true));
            AssetDatabase.CreateAsset(configObject, projectConfigFilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RecheckConventionDialog();
        }

        /// <summary>
        /// Manual call of the convention validation
        /// </summary>
        public static void RunConventionCheck()
        {
            if (config == null)
            {
                LoadConfigs();
            }

            if (active)
            {
                ProcessSubFolders("Assets");
            }
            else
            {
                EditorUtility.DisplayDialog("Oh no!", "The Convention Keeper Tool is turned off in the configuration file.", "Ok");
            }
        }

        /// <summary>
        /// Streamiline the evaluation process for the imported files
        /// </summary>
        /// <param name="filePath">the path of the file to be evaluated</param>
        /// <returns>The state of the given file</returns>
        public static FileConventionState CheckImportFileConvention(string filePath)
        {
            FileData file = new FileData(filePath);
            FolderConventionState folderState = CheckFolderConvention(file);
            FileConventionState fileState = FileConventionState.NotValid;

            switch (folderState)
            {
                case FolderConventionState.Valid:
                    fileState = CheckFileConvention(file);
                    break;
                case FolderConventionState.NotValid:
                    fileState = FileConventionState.NotValid;
                    break;
            }

            //Show warnings if any
            if (folderState == FolderConventionState.NotValid)
            {
                NotValidFolderDialog(file.folderAssetsPath);
            }
            else if (folderState == FolderConventionState.Valid && fileState == FileConventionState.NotValid)
            {
                switch (fileState)
                {
                    case FileConventionState.WrongFileName:
                        FileNameChangeDialog(file);
                        break;
                    case FileConventionState.NotValid:
                        //TODO - Dialog that allows dev to add file type in the config file
                        EditorUtility.DisplayDialog("OOOOPS!", "The file \"" + file.fullName + "\" is not format allowed by the convention.", "Ok");
                        break;
                }
            }

            return fileState;
        }

        /// <summary>
        /// Gets the rules of the given key
        /// </summary>
        /// <param name="key">The key to gather the rules for</param>
        /// <returns>The rules object</returns>
        public static JSONObject GetKeyRules(string key)
        {
            JSONObject rules = config["namingConvention"]["conventionKeyRules"].list.Find((JSONObject x) => x["key"].str == key)["rules"];
            if (rules == null)
            {
                Debug.LogError("There is no convention rules for the key: " + key);
            }
            return rules;
        }

        /// <summary>
        /// Checks if the file meets the naming convention criteria
        /// </summary>
        /// <param name="file">The file to be evaluated</param>
        /// <param name="conventions">the list of conventions for the given file type</param>
        /// <returns>The state of the file</returns>
        public static FileConventionState CheckFileNameConvention(FileData file, List<JSONObject> conventions)
        {
            foreach (JSONObject convention in conventions)
            {
                Regex matchRegex = new Regex("\\{\\w+\\}", RegexOptions.Compiled);
                string regexToMatch = string.Empty;
                foreach (Match key in matchRegex.Matches(convention.str))
                {
                    JSONObject rules = GetKeyRules(key.ToString());
                    if (!rules.IsNull)
                    {
                        regexToMatch += BuildRuleRegex(rules);
                    }
                }

                Match match = Regex.Match(file.fullName, regexToMatch);

                if (match.Success)
                {
                    return FileConventionState.Valid;
                }
                /*else
                {
                    Debug.LogError("[Match Problem] Value: " + match.Value + " | Regex to Match: " + regexToMatch);
                }*/
            }

            return FileConventionState.WrongFileName;
        }

        /// <summary>
        /// Checks if the file follows the convention
        /// </summary>
        /// <param name="file">The file data</param>
        /// <returns>The state of the file based on the convention</returns>
        public static FileConventionState CheckFileConvention(FileData file)
        {
            FileConventionState result = FileConventionState.NotValid;

            JSONObject pathObject = config["folderStructure"]["check"]["folders"].list.Find(x => x["path"].str == file.folderAssetsPath);

            if(pathObject == null)
            {
                return result;
            }

            if (pathObject["fileTypesAllowed"].list != null)
            {
                List<JSONObject> allowedFileTypes = new List<JSONObject>(pathObject["fileTypesAllowed"].list);

                JSONObject tmp = allowedFileTypes.Find(x => x.str == file.type);

                bool isAllowedFileType = (tmp != null) ? true : false;

                if (config["folderStructure"]["ignore"]["fileTypes"].list.Find(x => x.str == file.type) != null)
                {
                    result = FileConventionState.Ignored;
                }
                else if (isAllowedFileType)
                {
                    JSONObject conventionObject = GetNameConventionObject(file.type);
                    if (!conventionObject.IsNull)
                    {
                        List<JSONObject> typeConventions = conventionObject["conventions"].list;
                        if (typeConventions != null)
                        {
                            result = CheckFileNameConvention(file, typeConventions);
                        }
                    }
                    else
                    {
                        //TODO - There is not convention for the given file type!!! O.O
                        EditorUtility.DisplayDialog("OOOOPS!", "The file \"" + file.fullName + "\" is not format allowed by the convention." +
                                                               "\n" +
                                                               "You might want to add it in the configuration file.", "Ok");

                        result = FileConventionState.NotValid;
                    }
                }
                else
                {
                    result = FileConventionState.NotValid;
                }
            }

            return result;
        }

        /// <summary>
        /// Accurately checks if any given path is inside of a ignored folder and should be ignored
        /// </summary>
        /// <param name="path">The path to be evaluated</param>
        /// <returns>True if needs to be ignored or false if not</returns>
        public static bool CheckIgnoreFolder(string path)
        {
            foreach (JSONObject ignoredPath in config["folderStructure"]["ignore"]["folders"].list)
            {
                if (path.Contains(ignoredPath.str))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the folder path follows the convention
        /// </summary>
        /// <param name="file">The file that needs to be processed</param>
        /// <returns>The state of the folder based on the convention</returns>
        public static FolderConventionState CheckFolderConvention(FileData file)
        {
            FolderConventionState result = FolderConventionState.NotValid;
            if (CheckIgnoreFolder(file.folderAssetsPath))
            {
                result = FolderConventionState.Ignored;
            }
            else if (config["folderStructure"]["check"]["folders"].list.Find(x => x["path"].str == file.folderAssetsPath) != null)
            {
                result = FolderConventionState.Valid;
            }
            else
            {
                result = FolderConventionState.NotValid;
            }

            return result;
        }

        public static FolderConventionState CheckFolderConvention(string path)
        {
            FolderConventionState result = FolderConventionState.NotValid;
            if (CheckIgnoreFolder(path))
            {
                result = FolderConventionState.Ignored;
            }
            else if (config["folderStructure"]["check"]["folders"].list.Find(x => x["path"].str == path) != null)
            {
                result = FolderConventionState.Valid;
            }
            else
            {
                result = FolderConventionState.NotValid;
            }

            return result;
        }

        /// <summary>
        /// Recursively checks the conventions in folders and files
        /// </summary>
        /// <param name="path"></param>
        public static bool ProcessSubFolders(string path)
        {
            bool stopProcess = false;

            FolderConventionState folderState = CheckFolderConvention(path);

            switch (folderState)
            {
                case FolderConventionState.Ignored:
                    //Do nothing, ignore them :)
                    break;

                case FolderConventionState.Valid:
                    List<JSONObject> allowedFileTypes = config["folderStructure"]["check"]["folders"].list.Find(x => x["path"].str == path)["fileTypesAllowed"].list;

                    List<FileData> assetList = GetAllFilesDataAtPath(path);

                    if (allowedFileTypes != null && assetList.Count > 0)
                    {
                        string localErrorMessage = string.Empty;
                        foreach (FileData asset in assetList)
                        {
                            FileConventionState conventionState = CheckFileConvention(asset);

                            switch (conventionState)
                            {
                                case FileConventionState.WrongFileName:
                                    FileNameChangeDialog(asset);
                                    break;
                                case FileConventionState.NotValid:
                                    Dialog("The file \"" + asset.fullName + "\" is not following the convention. \nWhat to do?",
                                        "Add File Type to Folder Convention",
                                        delegate ()
                                        {
                                            //Adds file type to the current path allowed file types
                                            int indexOfFolderData = config["folderStructure"]["check"]["folders"].list.FindIndex(x => x["path"].str == asset.folderAssetsPath);
                                            config["folderStructure"]["check"]["folders"][indexOfFolderData]["fileTypesAllowed"].Add(asset.type);
                                            SaveConfigs();
                                        },
                                        "Add to Ignored File Types",
                                        delegate ()
                                        {
                                            //Write the file type into the ignored types of configuration file
                                            config["folderStructure"]["ignore"]["fileTypes"].Add(asset.type);
                                            SaveConfigs();
                                        },
                                        "Delete It!",
                                        delegate ()
                                        {
                                            DeleteDialog(asset);
                                        });
                                    break;
                            }

                            if (conventionState == FileConventionState.WrongFileName || conventionState == FileConventionState.NotValid)
                            {
                                stopProcess = true;
                            }
                        }
                    }
                    else if (allowedFileTypes == null && assetList.Count > 0)
                    {
                        Dialog("The path \"" + path + "\" has FILES which the TYPE IS NOT in the Convention, what to do?",
                                "Add FILE TYPES to this path Convention",
                                delegate ()
                                {
                                    //Write the file type into a folder in the configuration file
                                    int indexOfFolderData = config["folderStructure"]["check"]["folders"].list.FindIndex(x => x["path"].str == path);

                                    foreach (FileData asset in assetList)
                                    {
                                        config["folderStructure"]["check"]["folders"][indexOfFolderData]["fileTypesAllowed"].Add(asset.type);
                                    }

                                    SaveConfigs();
                                },
                                "Delete wrong FILES",
                                delegate ()
                                {
                                    //Delete files that are not in the allowed file types list
                                    DeleteDialog(delegate ()
                                    {
                                        foreach (FileData asset in assetList)
                                        {
                                            AssetDatabase.DeleteAsset(asset.assetsFullPath);
                                        }

                                        AssetDatabase.Refresh();

                                        RecheckConventionDialog();
                                    });
                                },
                                "Do nothing right now",
                                delegate ()
                                {
                                    //Do nothing - Highlight the parent folder just so the user can check it if wants
                                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
                                });

                        stopProcess = true;
                    }

                    //Check sub folders if allowed

                    if (allowedFileTypes == null) //There is no aloowed type in the folder
                    {
                        return false;
                    }

                    List<string> subFolders = new List<string>(AssetDatabase.GetSubFolders(path));
                    if (allowedFileTypes.Find(x => x.str == "folder") != null)
                    {
                        for (int i = 0; i < subFolders.Count; i++)
                        {
                            if (ProcessSubFolders(subFolders[i])) //If returns 1, means we need to stop the processing as the validation process might changed
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (subFolders.Count > 0)
                        {
                            Dialog("The path \"" + path + "\" does not allow FOLDERS and it contains " + subFolders.Count + " folders, what to do?",
                                "Add FOLDERS permission to this path",
                                delegate ()
                                {
                                    //Write the file type into a folder in the configuration file
                                    int indexOfFolderData = config["folderStructure"]["check"]["folders"].list.FindIndex(x => x["path"].str == path);
                                    config["folderStructure"]["check"]["folders"][indexOfFolderData]["fileTypesAllowed"].Add("folder");
                                    SaveConfigs();
                                },
                                "Delete FOLDERS",
                                delegate ()
                                {
                                    //Delete the list of sub folders
                                    DeleteDialog(delegate ()
                                    {
                                        foreach (string folder in subFolders)
                                        {
                                            AssetDatabase.DeleteAsset(folder);
                                        }

                                        AssetDatabase.Refresh();

                                        RecheckConventionDialog();
                                    });
                                },
                                "Do nothing right now",
                                delegate ()
                                {
                                    //Do nothing - Highlight the parent folder just so the user can check it if wants
                                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
                                });

                            stopProcess = true;
                        }
                    }
                    break;

                case FolderConventionState.NotValid:

                    NotValidFolderDialog(path);

                    stopProcess = true;

                    break;
            }

            return stopProcess;
        }

        public static void ClearOverview()
        {
            overviewFileList.Clear();
            overviewFolderList.Clear();
        }

        public static void OverviewProcessFolder(string path)
        {
            JSONObject pathConfig = config["folderStructure"]["check"]["folders"].list.Find(x => x["path"].str == path);

            List<JSONObject> allowedFileTypes = (pathConfig != null)? pathConfig["fileTypesAllowed"].list : null;

            List<FileData> assetList = GetAllFilesDataAtPath(path);

            FolderConventionState folderState = CheckFolderConvention(path);

            FileData folderData = new FileData(path);

            if(folderState == FolderConventionState.NotValid)
            {
                folderData.folderError.Add(FolderConventionState.NotValid);
            }

            List<string> subFolders = new List<string>(AssetDatabase.GetSubFolders(path));
            if (subFolders.Count > 0)
            {
                folderData.folderError.Add(FolderConventionState.NoFoldersAllowed);

                for (int i = 0; i < subFolders.Count; i++)
                {
                    OverviewProcessFolder(subFolders[i]);
                }
            }

            if (allowedFileTypes == null && assetList.Count > 0)
            {
                folderData.folderError.Add(FolderConventionState.UnknownFiles);
            }

            if (folderState != FolderConventionState.Ignored)
                overviewFolderList.Add(folderData);

            //Process Files
            if (assetList.Count > 0)
            {
                foreach (FileData asset in assetList)
                {
                    FileConventionState conventionState = CheckFileConvention(asset);

                    switch (conventionState)
                    {
                        case FileConventionState.WrongFileName:
                            asset.fileError.Add(FileConventionState.WrongFileName);
                            break;
                        case FileConventionState.NotValid:
                            asset.fileError.Add(FileConventionState.NotValid);

                            break;
                    }

                    overviewFileList.Add(asset);
                }
            }
        }

        /// <summary>
        /// Adds error text to a cache to be shown at the end of the validation process - Debug puposes
        /// </summary>
        /// <param name="error">The error message to be added</param>
        private static void AddError(string error)
        {
            folderErrors = folderErrors + "\n" + error;
        }

        /// <summary>
        /// Get all files paths in a given path
        /// </summary>
        /// <param name="path">The path to get the file paths from</param>
        /// <returns>A list of all file paths</returns>
        public static List<FileData> GetAllFilesDataAtPath(string path)
        {
            List<string> fileEntries = new List<string>(Directory.GetFiles(Application.dataPath + path.Remove(0, 6)));
            foreach (JSONObject type in config["folderStructure"]["ignore"]["fileTypes"].list)
            {
                fileEntries.RemoveAll(x => x.Contains("." + type.str));
            }

            List<FileData> tmpFileDataList = new List<FileData>();
            foreach (string filePath in fileEntries)
            {
                tmpFileDataList.Add(new FileData(filePath));
            }

            return tmpFileDataList;
        }

        /// <summary>
        /// Get the regex version of a config function
        /// </summary>
        /// <param name="ruleFunction">The config file function</param>
        /// <returns>Returns the regex version of a given config file function</returns>
        public static string GetRuleFunctionRegex(string ruleFunction)
        {
            string[] ruleData = ruleFunction.Split('(');
            string ruleRegex = "";

            JSONObject functionRegex = config["namingConvention"]["regexDictionary"].list.Find(x => x["function"].str == ruleData[0]);

            if (functionRegex != null)
            {
                ruleRegex = functionRegex["regex"].str;
                if (ruleRegex.Contains("KEY"))
                {
                    ruleRegex = ruleRegex.Replace("KEY", ruleData[1]);
                }
                return ruleRegex;
            }

            Debug.LogError("There is no rule for the Rule Function: " + ruleData[0]);

            return null;
        }

        /// <summary>
        /// Returns the complete regex from a given rule set
        /// </summary>
        /// <param name="rules">The set of rules that need to be translated to regex</param>
        /// <returns>The regex version of the given rules</returns>
        public static string BuildRuleRegex(JSONObject rules)
        {
            string ruleRegex = "(";
            string lastRule = string.Empty;

            foreach (JSONObject rule in rules.list)
            {
                string[] ruleData = rule.str.Split('(');

                if (ruleData[0] != lastRule && !CheckSufixPrefix(ruleData[0], lastRule))
                {
                    ruleRegex = ((!(lastRule == string.Empty)) ? (ruleRegex + ")(") : (ruleRegex + ((rules.list.Count == 1) ? "" : "(")));
                    ruleRegex += GetRuleFunctionRegex(rule.str);
                }
                else
                {
                    ruleRegex = ruleRegex + "|" + GetRuleFunctionRegex(rule.str);
                }

                lastRule = ruleData[0];

                if (rules.list.Last() == rule && rules.list.Count > 1)
                {
                    ruleRegex += ")";
                }
            }

            return ruleRegex + ")";
        }

        /// <summary>
        /// Checks if the arguments are sufix or prefix
        /// </summary>
        /// <returns>Returns true if both arguments are one of both</returns>
        public static bool CheckSufixPrefix(string first, string second)
        {
            if ((first == "sufix" || first == "prefix") && (second == "sufix" || second == "prefix"))
            {
                return true;
            }
            return false;
        }

        public static JSONObject GetNameConventionObject(string type)
        {
            JSONObject conventionObject = new JSONObject();

            foreach (JSONObject item in config["namingConvention"]["fileTypes"].list)
            {
                if (item["types"].list.Find(x => x.str == type) != null)
                {
                    conventionObject = item;
                    break;
                }
            }

            return conventionObject;
        }

        #region DIALOGS

        public static void Dialog(string message, string ok, Action okCallback)
        {
            if (EditorUtility.DisplayDialog(toolName + " " + toolVersion, message, ok))
            {
                if (okCallback != null)
                    okCallback.Invoke();
            }
        }

        public static void Dialog(string message, string ok, Action okCallback, string cancel, Action cancelCallback)
        {
            ConventionKeeperPopup.Dialog(toolName + " " + toolVersion, message, new ButtonData(ok, okCallback), new ButtonData(cancel, cancelCallback));
        }

        /// <summary>
        /// Opens a dialog with 3 options
        /// </summary>
        /// <param name="message">The message to be shown</param>
        /// <param name="ok">Ok button text</param>
        /// <param name="okCallback">Ok button callback</param>
        /// <param name="cancel">Cancel button text</param>
        /// <param name="cancelCallback">Cancel button callback</param>
        /// <param name="alt">Alt button text</param>
        /// <param name="altCallback">Alt button callback</param>
        public static void Dialog(string message, string ok, Action okCallback, string cancel, Action cancelCallback, string alt, Action altCallback)
        {
            ConventionKeeperPopup.Dialog(toolName + " " + toolVersion, message, new List<ButtonData>() { new ButtonData(ok, okCallback), new ButtonData(cancel, cancelCallback), new ButtonData(alt, altCallback) });
        }

        public static void DialogInputField(string message, string ok, Action<string> okCallback, string cancel, Action cancelCallback, FileData file)
        {
            ConventionKeeperPopup.DialogWithInputField(toolName + " " + toolVersion, message, file.fullName, new ButtonData(ok, okCallback), new ButtonData(cancel, cancelCallback));
        }

        public static void FileNameChangeDialog(FileData asset)
        {
            string conventionList = string.Empty;

            JSONObject conventionObject = GetNameConventionObject(asset.type);
            if (!conventionObject.IsNull)
            {
                foreach (JSONObject item in conventionObject["conventions"].list)
                {
                    conventionList += item.str + "\n";
                }
            }

            DialogInputField("The file \"" + asset.fullName + "\" does not match the convention criteria." +
                                             "\n" +
                                             "\nOne of these conventions will help:" +
                                             "\n" +
                                             "\n" + conventionList,
                            "Fix it", delegate (string newFileName)
                            {
                                string test = newFileName;
                                AssetDatabase.RenameAsset(asset.assetsFullPath, newFileName);
                                AssetDatabase.Refresh();
                                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.assetsFullPath.Replace(asset.fullName, newFileName)));

                                RecheckConventionDialog();
                            },
                            "Delete it", delegate ()
                            {
                                DeleteDialog(asset);
                            }, asset);
        }

        /// <summary>
        /// Delete dialog for deletion confirmation
        /// </summary>
        /// <param name="file">File to be deleted</param>
        public static void DeleteDialog(FileData file)
        {
            Dialog("Are you sure?\nThis action is not undoable!", "Yes", delegate ()
            {
                AssetDatabase.DeleteAsset(file.assetsFullPath);
                AssetDatabase.Refresh();

                RecheckConventionDialog();
            }, "No", null);
        }

        public static void DeleteDialog(Action callback)
        {
            Dialog("Are you sure?\nThis action is not undoable!", "Yes", callback, "No", null);
        }

        public static void RecheckConventionDialog()
        {
            bool autoRecheck = false;
            if (EditorPrefs.HasKey(autoRecheckKey))
            {
                autoRecheck = EditorPrefs.GetBool(autoRecheckKey);
            }

            if (!autoRecheck)
            {
                Dialog("Would you like to RECHECK the convetion?", "Yes :)", delegate ()
                {
                    RunConventionCheck();
                },
                "Always Recheck",
                delegate ()
                {
                    EditorPrefs.SetBool(autoRecheckKey, true);
                    RunConventionCheck();
                }, "No", null);
            }
            else
            {
                RunConventionCheck();
            }
        }

        public static void NotValidFolderDialog(string path)
        {
            Dialog("The path \"" + path + "\" is not in the Convention, what to do?",
                                "Add FOLDER to Convention",
                                delegate ()
                                {
                                    //Write the folder in the configuration file

                                    JSONObject folderObject = new JSONObject();
                                    folderObject.AddField("path", path);
                                    folderObject.AddField("fileTypesAllowed", new JSONObject("[]"));

                                    config["folderStructure"]["check"]["folders"].Add(folderObject);

                                    SaveConfigs();
                                },
                                "Ignore It",
                                delegate ()
                                {
                                    config["folderStructure"]["ignore"]["folders"].Add(path);
                                    SaveConfigs();
                                },
                                "Delete FOLDER",
                                delegate ()
                                {
                                    //Delete the folder
                                    DeleteDialog(delegate ()
                                    {
                                        AssetDatabase.DeleteAsset(path);
                                        AssetDatabase.Refresh();
                                    });
                                });
        }

        #endregion
    }
}
