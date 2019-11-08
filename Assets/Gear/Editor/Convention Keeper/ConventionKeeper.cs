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

        public FileData(string fullFilePath)
        {
            name = Path.GetFileNameWithoutExtension(fullFilePath);
            fullName = Path.GetFileName(fullFilePath);

            string extension = Path.GetExtension(fullFilePath);
            type = (extension != string.Empty) ? extension.Remove(0, 1) : "folder";
            assetsFullPath = fullFilePath.Remove(0, fullFilePath.IndexOf("Assets"));

            if (fullFilePath.Contains("\\"))
            {
                assetsFullPath = assetsFullPath.Replace("\\", "/");
            }

            folderAssetsPath = assetsFullPath.Remove(assetsFullPath.IndexOf("/" + name));
        }
    }

    [InitializeOnLoad]
    public class ConventionKeeper : MonoBehaviour
    {
        /// <summary>
        /// Holds the path to the configuration JSON file
        /// </summary>
        private const string configFilePath = "Assets/Gear/Config Files/ConventionKeeperConfig.json";

        private const string toolName = "Convention Keeper";

        private const string toolVersion = "v0.1";

        /// <summary>
        /// Holds the configuration JSON file data
        /// </summary>
        private static JSONObject config;

        /// <summary>
        /// Holds the folder structure
        /// </summary>
        private static JSONObject folderStructure;

        /// <summary>
        /// Holds the folder dictionary
        /// </summary>
        private static Dictionary<string, List<string>> folderDictionary;

        /// <summary>
        /// Holds the folders to be ignores
        /// </summary>
        private static List<string> ignoreFolders;

        /// <summary>
        /// Holds the files to be ignored
        /// </summary>
        private static List<string> ignoreFileTypes;

        /// <summary>
        /// Holds the folder errors message
        /// </summary>
        private static string folderErrors;

        /// <summary>
        /// Holds the file naming convention data
        /// </summary>
        private static JSONObject namingConvention;

        /// <summary>
        /// Holds the naming convention file types
        /// </summary>
        private static Dictionary<string, List<string>> fileTypes;

        /// <summary>
        /// Holds the convention rules
        /// </summary>
        private static List<JSONObject> conventionKeyRules;

        /// <summary>
        /// Holds the regex dictionary
        /// </summary>
        private static Dictionary<string, string> regexDictionary;

        /// <summary>
        /// Holds the state of the current tool, it might be deactivated
        /// </summary>
        public static bool active;

        /// <summary>
        /// Function called once when unity opens
        /// </summary>
        static ConventionKeeper()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the Convention Keeper Tool
        /// </summary>
        public static void Initialize()
        {
            config = new JSONObject();
            folderStructure = new JSONObject();
            folderDictionary = new Dictionary<string, List<string>>();
            ignoreFolders = new List<string>();
            ignoreFileTypes = new List<string>();
            namingConvention = new JSONObject();
            fileTypes = new Dictionary<string, List<string>>();
            conventionKeyRules = new List<JSONObject>();
            regexDictionary = new Dictionary<string, string>();
            active = false;
            LoadConfigs();
        }

        /// <summary>
        /// Loads the configuration JSON data into respective variables
        /// </summary>
        [MenuItem("Gear/Convention Checker/Reload Configs")]
        public static void LoadConfigs()
        {
            if (config != null)
            {
                config.Clear();
                folderStructure.Clear();
                folderDictionary.Clear();
                fileTypes.Clear();
                ignoreFolders.Clear();
                ignoreFileTypes.Clear();
                namingConvention.Clear();
                conventionKeyRules.Clear();
                regexDictionary.Clear();
                active = false;
            }

            UnityEngine.Object configFileData = AssetDatabase.LoadAssetAtPath("Assets/Gear/Config Files/ConventionKeeperConfig.json", typeof(UnityEngine.Object));

            config.Clear();
            config = new JSONObject(configFileData.ToString());
            if (!config["active"].b)
            {
                config.Clear();
                return;
            }

            active = true;

            folderStructure = config["folderStructure"];
            foreach (JSONObject folder in folderStructure["check"]["folders"].list)
            {
                if (!folderDictionary.ContainsKey(folder["path"].str))
                {
                    List<string> types = new List<string>();

                    foreach (JSONObject type in folder["fileTypesAllowed"].list)
                    {
                        types.Add(type.str);
                    }

                    folderDictionary.Add(folder["path"].str, types);
                }
            }

            foreach (JSONObject ignoredFolder in folderStructure["ignore"]["folders"].list)
            {
                ignoreFolders.Add(ignoredFolder.str);
            }

            ignoreFileTypes = new List<string>();
            foreach (JSONObject ignoredFileType in folderStructure["ignore"]["fileTypes"].list)
            {
                ignoreFileTypes.Add(ignoredFileType.str);
            }

            namingConvention = config["namingConvention"];
            foreach (JSONObject item in namingConvention["fileTypes"].list)
            {
                //Load Conventions
                List<string> conventions = new List<string>();
                foreach (JSONObject convention in item["conventions"].list)
                {
                    conventions.Add(convention.str);
                }

                //Load Types
                foreach (JSONObject type in item["types"].list)
                {
                    fileTypes.Add(type.str, conventions);
                }
            }

            conventionKeyRules = new List<JSONObject>(namingConvention["conventionKeyRules"].list);
            foreach (JSONObject item in namingConvention["regexDictionary"].list)
            {
                regexDictionary.Add(item["function"].str, item["regex"].str);
            }
        }

        /// <summary>
        /// Manual call of the convention validation
        /// </summary>
        [MenuItem("Gear/Convention Checker/Check Convention", priority = 0)]
        public static void RunConventionCheck()
        {
            if (active)
            {
                LoadConfigs();

                folderErrors = string.Empty;
                ProcessSubFolders("Assets");

                if (folderErrors != string.Empty)
                {
                    EditorUtility.DisplayDialog("Folder Convention Errors!", folderErrors, "Ok");
                }
                /*else
                {
                    EditorUtility.DisplayDialog("Good!", "No convention errors so far!", "Ok");
                }*/
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
                //EditorUtility.DisplayDialog("OOOOPS!", , "Ok");

                Dialog("The folder \"" + file.folderAssetsPath + "\" is not following the convention.", "Ok", null);
            }
            else if (folderState == FolderConventionState.Valid && fileState == FileConventionState.NotValid)
            {
                EditorUtility.DisplayDialog("OOOOPS!", "The file \"" + file.fullName + "\" is not following the convention.", "Ok");

                //FileNameChangeDialog(file);
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
            JSONObject rules = conventionKeyRules.Find((JSONObject x) => x["key"].str == key)["rules"];
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
        public static FileConventionState CheckFileNameConvention(FileData file, List<string> conventions)
        {
            foreach (string convention in conventions)
            {
                Regex matchRegex = new Regex("\\{\\w+\\}", RegexOptions.Compiled);
                string regexToMatch = string.Empty;
                foreach (Match key in matchRegex.Matches(convention))
                {
                    JSONObject rules = GetKeyRules(key.ToString());
                    if (!rules.IsNull)
                    {
                        regexToMatch += BuildRuleRegex(rules);
                    }
                }
                if (Regex.Match(file.fullName, regexToMatch).Success)
                {
                    return FileConventionState.Valid;
                }
            }
            return FileConventionState.NotValid;
        }

        /// <summary>
        /// Checks if the file follows the convention
        /// </summary>
        /// <param name="file">The file data</param>
        /// <returns>The state of the file based on the convention</returns>
        public static FileConventionState CheckFileConvention(FileData file)
        {
            FileConventionState result = FileConventionState.NotValid;
            List<string> allowedFileTypes = folderDictionary[file.folderAssetsPath];
            if (ignoreFileTypes.Contains(file.type))
            {
                result = FileConventionState.Ignored;
            }
            else if (allowedFileTypes.Contains(file.type))
            {
                List<string> typeConventions = fileTypes[file.type];
                if (typeConventions != null)
                {
                    result = CheckFileNameConvention(file, typeConventions);
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
            foreach (string ignoredPath in ignoreFolders)
            {
                if (path.Contains(ignoredPath))
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
            else if (folderDictionary.ContainsKey(file.folderAssetsPath))
            {
                result = FolderConventionState.Valid;
            }
            return result;
        }

        /// <summary>
        /// Recursively checks the conventions in folders and files
        /// </summary>
        /// <param name="path"></param>
        public static void ProcessSubFolders(string path)
        {
            if (ignoreFolders.Contains(path))
            {
                //Do nothing, ignore them :)
                return;
            }
            else if (folderDictionary.ContainsKey(path))
            {
                List<string> allowedFileTypes = folderDictionary[path];
                List<FileData> assetList = GetAllFilesDataAtPath(path);
                if (allowedFileTypes != null && assetList.Count > 0)
                {
                    string localErrorMessage = string.Empty;
                    foreach (FileData asset in assetList)
                    {
                        FileConventionState conventionState = CheckFileConvention(asset);
                        if (conventionState == FileConventionState.NotValid)
                        {
                            FileNameChangeDialog(asset);
                        }
                    }
                    if (localErrorMessage != string.Empty)
                    {
                        string msg = localErrorMessage;
                        localErrorMessage = "\nPath \"" + path + "\" issues:" + msg + "\n";
                        AddFolderError(localErrorMessage);
                    }
                }
                else if (allowedFileTypes == null && assetList.Count > 0)
                {
                    AddFolderError("The path: " + path + " has FILES which the TYPE IS NOT in the Convention File.");
                }
                if (allowedFileTypes == null)
                {
                    return;
                }
                List<string> subFolders = new List<string>(AssetDatabase.GetSubFolders(path));
                if (allowedFileTypes.Contains("folder"))
                {
                    for (int i = 0; i < subFolders.Count; i++)
                    {
                        ProcessSubFolders(subFolders[i]);
                    }
                }
                else if (subFolders.Count > 0)
                {
                    AddFolderError("The path: " + path + " has FOLDERS that are NOT allowed in the Convention File:");
                    foreach (string item in subFolders)
                    {
                        AddFolderError(item);
                    }
                }
            }
        }

        /// <summary>
        /// Adds error text to a cache to be shown at the end of the validation process - Debug puposes
        /// </summary>
        /// <param name="error">The error message to be added</param>
        private static void AddFolderError(string error)
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
            foreach (string type in ignoreFileTypes)
            {
                fileEntries.RemoveAll((string x) => x.Contains("." + type));
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
            string ruleRegex2 = "";

            if (regexDictionary.ContainsKey(ruleData[0]))
            {
                ruleRegex2 = regexDictionary[ruleData[0]];
                if (ruleRegex2.Contains("KEY"))
                {
                    ruleRegex2 = ruleRegex2.Replace("KEY", ruleData[1]);
                }
                return ruleRegex2;
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

        #region DIALOGS

        public static void Dialog(string message, string ok, Action okCallback)
        {
            if (EditorUtility.DisplayDialog(toolName + " " + toolVersion, message, ok))
            {
                if (okCallback != null)
                    okCallback.Invoke();
            }
        }

        public static void DialogTwoOptions(string message, string ok, Action okCallback, string cancel, Action cancelCallback)
        {
            ConventionKeeperPopup.Dialog(toolName + " " + toolVersion, message, new ButtonData(ok, okCallback), new ButtonData(cancel, cancelCallback));
            
            /*if (EditorUtility.DisplayDialog(toolName + " " + toolVersion, message, ok, cancel))
            {
                if (okCallback != null)
                    okCallback.Invoke();
            }
            else
            {
                if (cancelCallback != null)
                    cancelCallback.Invoke();
            }*/
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
        public static void DialogThreeOptions(string message, string ok, Action okCallback, string cancel, Action cancelCallback, string alt, Action altCallback)
        {
            switch (EditorUtility.DisplayDialogComplex(toolName + " " + toolVersion, message, ok, cancel, alt))
            {
                //ok
                case 0:
                    if (okCallback != null)
                        okCallback.Invoke();
                    break;
                //cancel
                case 1:
                    if (cancelCallback != null)
                        cancelCallback.Invoke();
                    break;
                //alt
                case 2:
                    if (altCallback != null)
                        altCallback.Invoke();
                    break;
            }
        }

        public static void DialogInputField(string message, string ok, Action<string> okCallback, string cancel, Action cancelCallback, FileData file)
        {
            ConventionKeeperPopup.DialogWithInputField(toolName + " " + toolVersion, message, file.fullName, new ButtonData(ok, okCallback), new ButtonData(cancel, cancelCallback));
        }

        public static void FileNameChangeDialog(FileData asset)
        {
            string conventionList = string.Empty;

            foreach (string item in fileTypes[asset.type])
            {
                conventionList += item + "\n";
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
            DialogTwoOptions("Are you sure?\nThis action is not undoable!", "Yes", delegate ()
            {
                AssetDatabase.DeleteAsset(file.assetsFullPath);
                AssetDatabase.Refresh();
            }, "No", null);
        }

        #endregion
    }
}
