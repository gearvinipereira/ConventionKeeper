/* Convention Keeper Tool - v0.3 - By Vinicius Pereira - Gear Inc.
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

    #region Data Types

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

        /// <summary>
        /// Constructor of the FileData class
        /// </summary>
        /// <param name="fullFilePath">Path of the file that will be turned into a FileData object.</param>
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

    #endregion

    [InitializeOnLoad]
    public class ConventionKeeper : MonoBehaviour
    {
        #region Statical/Constant Variables

        /// <summary>
        /// The name of the tool
        /// </summary>
        public const string toolName = "Convention Keeper";

        /// <summary>
        /// The version of the tool
        /// </summary>
        public const string toolVersion = "v0.3";

        /// <summary>
        /// Holds the path to the base configuration JSON
        /// </summary>
        private const string baseConfigFilePath = "Assets/Gear/Config Files/ConventionKeeperConfigBase.json";

        /// <summary>
        /// Holds the path to the current project configuration JSON
        /// </summary>
        private static string projectConfigFilePath = "Assets/Gear/Config Files/ConventionKeeper_{NAME}_Config.asset";

        #endregion

        #region Editor Prefs Variables

        /// <summary>
        /// Holds the key to the Editor.Prefs autoRecheck variable
        /// </summary>
        private const string autoRecheckKey = "AutoRecheckAfterDialog";

        /// <summary>
        /// Holds the key to the Editor.Prefs setupDone variable
        /// </summary>
        public const string setupDoneKey = "isSetupDone";

        #endregion

        #region Tool Runtime Variables

        /// <summary>
        /// Holds the configuration JSON file data
        /// </summary>
        public static JSONObject config = null;

        /// <summary>
        /// Holds the state of the current tool, it might be deactivated
        /// </summary>
        public static bool active = false;

        #endregion

        #region Overview Variables

        /// <summary>
        /// A flag to bypass some checks once in overview screen
        /// </summary>
        public static bool usingOverview = false;

        /// <summary>
        /// List of folders with convention validation problems
        /// </summary>
        public static List<FileData> overviewFolderList = new List<FileData>();

        /// <summary>
        /// List of files with convention validation problems
        /// </summary>
        public static List<FileData> overviewFileList = new List<FileData>();

        #endregion

        #region Starting Functions
        /// <summary>
        /// Function called once when unity opens
        /// </summary>
        static ConventionKeeper()
        {
            if (!ConventionKeeper.CheckIfConfigFileExists() && config == null)
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
            if (!usingOverview)
            {
                //Setup config path with app name
                projectConfigFilePath = projectConfigFilePath.Replace("{NAME}", Application.productName);

                if (EditorPrefs.HasKey(setupDoneKey))
                {
                    if (EditorPrefs.GetBool(setupDoneKey)) //If the setup was done the Convention Keeper runs, if not it assumes its not to.
                    {
                        RunConventionCheck();
                    }
                    else if (CheckIfConfigFileExists())
                    {
                        EditorPrefs.SetBool(setupDoneKey, true);

                        RunConventionCheck();
                    }
                }
                else
                {
                    ConventionKeeperPopup.FirstTimeDialog(500, 150);
                }
            }
        }

        /// <summary>
        /// Fakes an auto start, like when unity opens
        /// </summary>
        //[MenuItem("Gear/Convention Checker/Fake Auto Start")]
        public static void FakeAutoStart()
        {
            if (config != null)
            {
                ConfigClear();
                config = null;
            }

            Initialize();
        }

        /// <summary>
        /// Fakes a fresh start, the first time the tool is installed in the project
        /// </summary>
        //[MenuItem("Gear/Convention Checker/Fake Fresh Start")]
        public static void FakeFreshStart()
        {
            if (config != null)
            {
                ConfigClear();
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

            usingOverview = false;

            AssetDatabase.DeleteAsset(projectConfigFilePath);

            Initialize();
        }

        /// <summary>
        /// Generates the base configuration file with the project name, good for hand edition of it before loading for the first time.
        /// </summary>
        [MenuItem("Gear/Convention Checker/Generate New Config File")]
        public static void GenerateConfigFile()
        {
            if (EditorPrefs.HasKey(ConventionKeeper.setupDoneKey))
            {
                EditorPrefs.DeleteKey(ConventionKeeper.setupDoneKey);
            }

            TextAsset configTextAsset;

            UnityEngine.Object configFileData = AssetDatabase.LoadAssetAtPath(baseConfigFilePath, typeof(UnityEngine.Object));

            configTextAsset = new TextAsset(configFileData.ToString());

            AssetDatabase.CreateAsset(configTextAsset, projectConfigFilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Setup any necessary variables, save necessary files.
        /// </summary>
        public static void SetupFirstTime()
        {
            GenerateConfigFile();

            LoadConfigs();

            RefreshOverview();

            OpenOverview();
        }

        #endregion

        #region Overview Functions

        /// <summary>
        /// Opens the Overview window.
        /// </summary>
        [MenuItem("Gear / Convention Checker / Open Overview", priority = 1)]
        public static void OpenOverview()
        {
            usingOverview = true;

            RefreshOverview();

            ConventionKeeperPopup.OverviewDialog();
        }

        /// <summary>
        /// Clears the overview file and folder lists
        /// </summary>
        public static void ClearOverview()
        {
            overviewFileList.Clear();
            overviewFolderList.Clear();
        }

        /// <summary>
        /// Processes all folders under the given path seeking convention breaches.
        /// </summary>
        /// <param name="path">The path to process to use results on Overview Window</param>
        public static void OverviewProcessFolder(string path)
        {
            JSONObject pathConfig = config["folderStructure"]["check"]["folders"].list.Find(x => x["path"].str == path);

            List<JSONObject> allowedFileTypes = (pathConfig != null) ? pathConfig["fileTypesAllowed"].list : null;

            List<FileData> assetList = GetAllFileDataAtPath(path);

            FolderConventionState folderState = CheckFolderConvention(path);

            if (folderState != FolderConventionState.Ignored)
            {
                FileData folderData = new FileData(path);

                if (folderState == FolderConventionState.NotValid)
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

                if (folderState != FolderConventionState.Ignored && folderState != FolderConventionState.Valid)
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
                                overviewFileList.Add(asset);
                                break;
                            case FileConventionState.NotValid:
                                asset.fileError.Add(FileConventionState.NotValid);
                                overviewFileList.Add(asset);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Refreshes the overview file and folder lists (Reprocess all folders for convention breaches)
        /// </summary>
        public static void RefreshOverview()
        {
            ConventionKeeper.ClearOverview();
            ConventionKeeper.OverviewProcessFolder("Assets");
        }

        #endregion

        #region Config Management Functions

        /// <summary>
        /// Clears the config variable, reset necessary variables too.
        /// </summary>
        public static void ConfigClear()
        {
            config.Clear();
            config = null;
            active = false;
        }

        /// <summary>
        /// Loads the configuration JSON data into config variable
        /// </summary>
        [MenuItem("Gear/Convention Checker/Reload Configs")]
        public static void LoadConfigs()
        {
            if (config != null)
            {
                ConfigClear();
            }

            TextAsset configFileData = (TextAsset)AssetDatabase.LoadAssetAtPath(projectConfigFilePath, typeof(TextAsset));

            if (configFileData == null)
            {
                usingOverview = false;
                if (EditorPrefs.HasKey(setupDoneKey))
                {
                    EditorPrefs.DeleteKey(setupDoneKey);
                }

                Initialize();
                return;
            }

            config = new JSONObject(configFileData.text);

            active = config["active"].b;

            if (!active)
            {
                config.Clear();
                return;
            }
        }

        /// <summary>
        /// Saves the config variable data into a config asset file.
        /// </summary>
        public static void SaveConfigs()
        {
            TextAsset configObject = new TextAsset(config.ToString(true));
            AssetDatabase.CreateAsset(configObject, projectConfigFilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!usingOverview)
            {
                RecheckConventionDialog();
            }
            else
            {
                RefreshOverview();
            }
        }

        /// <summary>
        /// Checks if the config file of the current project exists in the Config Files directory
        /// </summary>
        /// <returns></returns>
        public static bool CheckIfConfigFileExists()
        {
            string path = Application.dataPath.Remove(Application.dataPath.IndexOf("Assets")) + projectConfigFilePath;
            return File.Exists(path);
        }

        #endregion

        #region Convention Validation Functions

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
                        DialogNoValidFile(file);
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

        /// <summary>
        /// Finds the type name convention object
        /// </summary>
        /// <param name="type">The type to search for a convention object</param>
        /// <returns>Returns the given type convention object, if any.</returns>
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
            }

            return FileConventionState.WrongFileName;
        }

        /// <summary>
        /// Gather all valid file types until the root folder of the path.
        /// </summary>
        /// <param name="path">The path to get gather the types for.</param>
        /// <returns>A list of accpeted types on tha path.</returns>
        public static List<string> GetAllTypesOfPath(string path)
        {
            List<string> typeList = new List<string>();

            int pathFolderNumber = path.Count(x => x == '/');

            pathFolderNumber = (pathFolderNumber == 0) ? 1 : pathFolderNumber;

            string lastPath = path;

            for (int i = 0; i < pathFolderNumber; i++)
            {
                JSONObject pathObject = config["folderStructure"]["check"]["folders"].list.Find(x => x["path"].str == lastPath);

                if (pathObject != null)
                {
                    foreach (JSONObject type in pathObject["fileTypesAllowed"].list)
                    {
                        if (!typeList.Contains(type.str))
                        {
                            typeList.Add(type.str);
                        }
                    }
                }

                string newLAstPath = Path.GetDirectoryName(lastPath);

                newLAstPath = newLAstPath.Replace("\\", "/");

                lastPath = newLAstPath;
            }

            return typeList;
        }

        /// <summary>
        /// Checks if the file follows the convention
        /// </summary>
        /// <param name="file">The file data</param>
        /// <returns>The state of the file based on the convention</returns>
        public static FileConventionState CheckFileConvention(FileData file)
        {
            FileConventionState result = FileConventionState.NotValid;

            //We run through all folders until the root, collecting possible valid file types. If a parent accept, the children should too.
            List<string> allowedFileTypesList = GetAllTypesOfPath(file.folderAssetsPath);

            if (config["folderStructure"]["ignore"]["files"].list.Find(x => x.str == file.fullName) != null)
            {
                result = FileConventionState.Ignored;
            }
            else if (allowedFileTypesList.Count == 0)
            {
                result = FileConventionState.NotValid;
            }
            else if (allowedFileTypesList.Count > 0)
            {
                string tmp = allowedFileTypesList.Find(x => x == file.type);

                bool isAllowedFileType = (tmp != null) ? true : false;

                if (!isAllowedFileType)
                {
                    if (config["folderStructure"]["ignore"]["fileTypes"].list.Find(x => x.str == file.type) != null)
                    {
                        result = FileConventionState.Ignored;
                    }
                    else
                    {
                        result = FileConventionState.NotValid;
                    }
                }
                else
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
                        result = FileConventionState.NotValid;
                    }
                }
            }
            else
            {
                result = FileConventionState.UnknownFileType;
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
        /// Checks if the folder is child of any valid path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool CheckFolderIsValidChild(string path)
        {
            JSONObject validParentPath = config["folderStructure"]["check"]["folders"].list.Find(x => path.Contains(x["path"].str));

            if (validParentPath != null)
            {
                if (validParentPath["fileTypesAllowed"].list.Find(x => x.str == "folder") != null)
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
            else if (CheckFolderIsValidChild(file.folderAssetsPath))
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
        /// Checks if the given folder path is in the convention.
        /// </summary>
        /// <param name="path">The folder path to be evaluated.</param>
        /// <returns>Returns the state of the given folder.</returns>
        public static FolderConventionState CheckFolderConvention(string path)
        {
            FolderConventionState result = FolderConventionState.NotValid;
            if (CheckIgnoreFolder(path))
            {
                result = FolderConventionState.Ignored;
            }
            else if (CheckFolderIsValidChild(path))
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
        /// Checks if the given file type is a valid file type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsValidFileType(string type)
        {
            //Checks if the given type is on the ignored file types, if so, it is considered a valid type as it is being tracked.
            foreach (JSONObject item in ConventionKeeper.config["folderStructure"]["ignore"]["fileTypes"].list)
            {
                if (item.str == type)
                {
                    return true;
                }
            }

            //Checks if the given type is on the expected file types, if so, it is considered a valid type as it is being tracked.
            foreach (JSONObject item in ConventionKeeper.config["namingConvention"]["fileTypes"].list)
            {
                if (item["types"].list.Find(x => x.str == type) != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get all files paths in a given path
        /// </summary>
        /// <param name="path">The path to get the file paths from</param>
        /// <returns>A list of all file paths</returns>
        public static List<FileData> GetAllFileDataAtPath(string path)
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
                    List<string> allowedFileTypes = GetAllTypesOfPath(path);

                    List<FileData> assetList = GetAllFileDataAtPath(path);

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
                                    DialogNoValidFile(asset);
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
                                new List<ButtonData>() {
                                    new ButtonData("Add FILE TYPES to this path Convention",
                                    delegate ()
                                    {
                                        AddFileTypesToPath(path, assetList);
                                    }),
                                    new ButtonData("Delete wrong FILES",
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
                                    }),
                                    new ButtonData("Do nothing right now",
                                    delegate ()
                                    {
                                        //Do nothing - Highlight the parent folder just so the user can check it if wants
                                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
                                    })
                                });

                        stopProcess = true;
                    }

                    //Check sub folders if allowed

                    if (allowedFileTypes == null) //There is no aloowed type in the folder
                    {
                        return false;
                    }

                    List<string> subFolders = new List<string>(AssetDatabase.GetSubFolders(path));
                    if (allowedFileTypes.Contains("folder"))
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
                                new List<ButtonData>() {
                                    new ButtonData("Add FOLDERS permission to this path",
                                    delegate ()
                                    {
                                        AddFolderTypeToPath(path);
                                    }),
                                    new ButtonData("Delete child FOLDERS",
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
                                    }),
                                    new ButtonData("Do nothing right now",
                                    delegate ()
                                    {
                                        //Do nothing - Highlight the parent folder just so the user can check it if wants
                                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
                                    })
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
        #endregion

        #region Error Handling Functions

        public static void AddToIgnoredFiles(FileData asset)
        {
            //Write the file type into the ignored files of configuration file
            config["folderStructure"]["ignore"]["files"].Add(asset.fullName);
            SaveConfigs();
        }

        /// <summary>
        /// Adds the given file asset type to the ignored file types list.
        /// </summary>
        /// <param name="asset">The FileData of the asset to be ignored.</param>
        public static void AddToIgnoredFileTypes(FileData asset)
        {
            //Write the file type into the ignored types of configuration file
            config["folderStructure"]["ignore"]["fileTypes"].Add(asset.type);
            SaveConfigs();
        }

        /// <summary>
        /// Adds the given file asset type to the given asset path.
        /// </summary>
        /// <param name="asset">The FileData of the asset to be added.</param>
        public static void AddFileTypeToPath(FileData asset)
        {
            //Adds file type to the current path allowed file types
            int indexOfFolderData = config["folderStructure"]["check"]["folders"].list.FindIndex(x => x["path"].str == asset.folderAssetsPath);
            config["folderStructure"]["check"]["folders"][indexOfFolderData]["fileTypesAllowed"].Add(asset.type);
            SaveConfigs();
        }

        /// <summary>
        /// Adds all the child of a path files types to the given path.
        /// </summary>
        /// <param name="path">The path to be evaluated and have the files added to.</param>
        /// <param name="assetList">Assets to be added to the given path.</param>
        public static void AddFileTypesToPath(string path, List<FileData> assetList)
        {
            //Write the file type into a folder in the configuration file
            int indexOfFolderData = config["folderStructure"]["check"]["folders"].list.FindIndex(x => x["path"].str == path);

            foreach (FileData asset in assetList)
            {
                if (config["folderStructure"]["check"]["folders"][indexOfFolderData]["fileTypesAllowed"].list.Find(x => x.str == asset.type) == null)
                {
                    config["folderStructure"]["check"]["folders"][indexOfFolderData]["fileTypesAllowed"].Add(asset.type);
                }
            }

            SaveConfigs();
        }

        /// <summary>
        /// Adds the exclusive "folder" type to the folder making possible to have folder in the given path.
        /// </summary>
        /// <param name="path">The path to have the "folder" type added to.</param>
        public static void AddFolderTypeToPath(string path)
        {
            //Write the file type into a folder in the configuration file
            int indexOfFolderData = config["folderStructure"]["check"]["folders"].list.FindIndex(x => x["path"].str == path);
            config["folderStructure"]["check"]["folders"][indexOfFolderData]["fileTypesAllowed"].Add("folder");
            SaveConfigs();
        }

        /// <summary>
        /// Adds a path to the ignored paths list.
        /// </summary>
        /// <param name="path">The path to be added to the ignore list.</param>
        public static void AddToIgnoredPaths(string path)
        {
            config["folderStructure"]["ignore"]["folders"].Add(path);
            SaveConfigs();
        }

        /// <summary>
        /// Adds a path to the current project convention.
        /// </summary>
        /// <param name="path">The path to be added to the project convention tracking list.</param>
        public static void AddPathToConvention(string path)
        {
            //Write the folder in the configuration file

            JSONObject folderObject = new JSONObject();
            folderObject.AddField("path", path);
            folderObject.AddField("fileTypesAllowed", new JSONObject("[]"));

            config["folderStructure"]["check"]["folders"].Add(folderObject);

            SaveConfigs();
        }

        /// <summary>
        /// Deletes a folder after confirmation.
        /// </summary>
        /// <param name="path">The path of the folder to delete.</param>
        public static void DeleteFolder(string path)
        {
            //Delete the folder
            DeleteDialog(delegate ()
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.Refresh();
            });
        }

        #endregion

        #region DIALOGS

        /// <summary>
        /// Shows a N input button dialog.
        /// </summary>
        /// <param name="message">Dialog message.</param>
        /// <param name="buttonList">List of ButtonData to add to the dialog</param>
        public static void Dialog(string message, List<ButtonData> buttonList, int width = -1, int height = -1, Color customBGColor = new Color())
        {
            ConventionKeeperPopup.Dialog(toolName + " " + toolVersion, message, buttonList, width, height, customBGColor);
        }

        public static void DialogNoValidFile(FileData asset)
        {
            Dialog("The file \"" + asset.fullName + "\" is not following the convention. \nWhat to do?",
                new List<ButtonData>() {
                    new ButtonData("Add File Type to Folder Convention",
                    delegate ()
                    {
                        AddFileTypeToPath(asset);
                    }),
                    new ButtonData("Add to Ignored Files",
                    delegate ()
                    {
                        AddToIgnoredFiles(asset);
                    }),
                    new ButtonData("Add to Ignored File Types",
                    delegate ()
                    {
                        AddToIgnoredFileTypes(asset);
                    }),
                    new ButtonData("Delete It",
                    delegate ()
                    {
                        DeleteDialog(asset);
                    })
                });
        }

        /// <summary>
        /// Opens a special dialog with 2 input buttons and 1 text input field to change a file property.
        /// </summary>
        /// <param name="message">Dialog Mmessage.</param>
        /// <param name="ok">First button label.</param>
        /// <param name="okCallback">First button callback.</param>
        /// <param name="cancel">Second button label.</param>
        /// <param name="cancelCallback">Second button callback.</param>
        /// <param name="file">File to be altered.</param>
        public static void DialogInputField(string message, string ok, Action<string> okCallback, string cancel, Action cancelCallback, FileData file)
        {
            ConventionKeeperPopup.DialogWithInputField(toolName + " " + toolVersion, message, file.fullName, new ButtonData(ok, okCallback), new ButtonData(cancel, cancelCallback));
        }

        /// <summary>
        /// Opens a File Name Change Dialog.
        /// </summary>
        /// <param name="asset">The asset FileData to have its name changed.</param>
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
                                if (!ConventionKeeper.usingOverview)
                                {
                                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.assetsFullPath.Replace(asset.fullName, newFileName)));
                                    RecheckConventionDialog();
                                }
                            },
                            "Delete it", delegate ()
                            {
                                DeleteDialog(asset);
                            }, asset);
        }

        /// <summary>
        /// Dialog for a specific file deletion confirmation.
        /// </summary>
        /// <param name="file">File to be deleted.</param>
        public static void DeleteDialog(FileData file)
        {
            Dialog("Are you sure?\nThis action is not undoable!", new List<ButtonData>() { new ButtonData("Yes", delegate ()
            {
                AssetDatabase.DeleteAsset(file.assetsFullPath);
                AssetDatabase.Refresh();

                RecheckConventionDialog();

                if(usingOverview)
                {
                    RefreshOverview();
                }
            }), new ButtonData("No", (Action)null) }, 250, 150, new Color(1, 0, 0, 0.25f));
        }

        /// <summary>
        /// Dialog for deletion confirmation.
        /// </summary>
        /// <param name="callback">The callback to be invoked if user confirms deletion.</param>
        public static void DeleteDialog(Action callback)
        {
            Dialog("Are you sure?\nThis action is not undoable!", new List<ButtonData>() { new ButtonData("Yes", callback), new ButtonData("No", (Action)null) }, 250, 150, new Color(1, 0, 0, 0.25f));
        }

        /// <summary>
        /// Dialog to ask if the user want to rerun the convention validation.
        /// </summary>
        public static void RecheckConventionDialog()
        {
            if (!usingOverview)
            {
                bool autoRecheck = false;
                if (EditorPrefs.HasKey(autoRecheckKey))
                {
                    autoRecheck = EditorPrefs.GetBool(autoRecheckKey);
                }

                if (!autoRecheck)
                {
                    Dialog("Would you like to RECHECK the convetion?", new List<ButtonData>() { new ButtonData("Yes :)", delegate ()
                    {
                        RunConventionCheck();
                    }),
                    new ButtonData("Always Recheck",
                    delegate ()
                    {
                        EditorPrefs.SetBool(autoRecheckKey, true);
                        RunConventionCheck();
                    }), new ButtonData("No", (Action)null) });
                }
                else
                {
                    RunConventionCheck();
                }
            }
        }

        /// <summary>
        /// Dialog to handle folder errors.
        /// </summary>
        /// <param name="path">The path of the folder with errors.</param>
        public static void NotValidFolderDialog(string path)
        {
            Dialog("The path \"" + path + "\" is not in the Convention, what to do?",
                new List<ButtonData>() {
                    new ButtonData("Add FOLDER to Convention",
                    delegate ()
                    {
                        AddPathToConvention(path);
                    }),
                    new ButtonData("Ignore It",
                    delegate ()
                    {
                        AddToIgnoredPaths(path);
                    }),
                    new ButtonData("Delete FOLDER",
                    delegate ()
                    {
                        DeleteFolder(path);
                    })
                });
        }
        #endregion
    }
}
