using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Gear.Tools.ConventionKeeper
{
    public enum ConventionState
    {
        Valid,
        Ignored,
        NotValid
    }

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
        private const string configFilePath = "Assets/Gear/Config Files/ConventionKeeperConfig.json";

        private static JSONObject config;

        private static JSONObject folderStructure;

        private static Dictionary<string, List<JSONObject>> folderDictionary;

        private static List<string> ignoreFolders;

        private static List<JSONObject> ignoreFileTypes;

        private static string folderErrors;

        private static JSONObject namingConvention;

        private static List<JSONObject> fileTypes;

        private static List<JSONObject> conventionKeyRules;

        private static Dictionary<string, string> regexDictionary;

        private static bool active;

        static ConventionKeeper()
        {
            config = new JSONObject();
            folderStructure = new JSONObject();
            folderDictionary = new Dictionary<string, List<JSONObject>>();
            ignoreFolders = new List<string>();
            ignoreFileTypes = new List<JSONObject>();
            namingConvention = new JSONObject();
            fileTypes = new List<JSONObject>();
            conventionKeyRules = new List<JSONObject>();
            regexDictionary = new Dictionary<string, string>();
            active = true;
            LoadConfigs();
        }

        [MenuItem("Gear/Convention Checker/Check Convention")]
        public static void RunConventionCheck()
        {
            if (!active)
            {
                LoadConfigs();
            }
            folderErrors = string.Empty;
            ProcessSubFolders("Assets");
            if (folderErrors != null)
            {
                EditorUtility.DisplayDialog("Folder Convention Errors!", folderErrors, "Ok");
            }
        }

        public static ConventionState CheckImportFileConvention(string filePath)
        {
            FileData file = new FileData(filePath);
            ConventionState result = CheckFolderConvention(file);
            switch (result)
            {
                case ConventionState.Valid:
                    result = CheckFileConvention(file);
                    break;
                case ConventionState.NotValid:
                    EditorUtility.DisplayDialog("OOOOPS!", "The file \"" + file.assetsFullPath + file.fullName + "\" is not following the convention.", "NHHEEEE");
                    result = ConventionState.NotValid;
                    break;
            }
            return result;
        }

        public static JSONObject GetKeyRules(string key)
        {
            JSONObject rules = conventionKeyRules.Find((JSONObject x) => x["key"].str == key)["rules"];
            if (rules == null)
            {
                Debug.LogError("There is no convention rules for the key: " + key);
            }
            return rules;
        }

        public static ConventionState CheckFileNameConvention(FileData file, List<JSONObject> conventions)
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
                if (Regex.Match(file.fullName, regexToMatch).Success)
                {
                    return ConventionState.Valid;
                }
            }
            return ConventionState.NotValid;
        }

        public static ConventionState CheckFileConvention(FileData file)
        {
            ConventionState result = ConventionState.NotValid;
            List<JSONObject> allowedFileTypes = folderDictionary[file.folderAssetsPath];
            if (ignoreFileTypes.Find((JSONObject x) => x.str == file.type) != null)
            {
                result = ConventionState.Ignored;
            }
            else if (allowedFileTypes.Find((JSONObject x) => x.str == file.type) != null)
            {
                JSONObject typeConvention = fileTypes.Find((JSONObject x) => x["types"].list.Find((JSONObject y) => y.str == file.type));
                if (typeConvention != null)
                {
                    result = CheckFileNameConvention(file, typeConvention["conventions"].list);
                }
            }
            return result;
        }

        public static bool CheckIgnoreFolder(string path)
        {
            foreach (string ignoredPath in ignoreFolders)
            {
                if(path.Contains(ignoredPath))
                {
                    return true;
                }
            }

            return false;
        }

        public static ConventionState CheckFolderConvention(FileData file)
        {
            ConventionState result = ConventionState.NotValid;
            if (CheckIgnoreFolder(file.folderAssetsPath))
            {
                result = ConventionState.Ignored;
            }
            else if (folderDictionary.ContainsKey(file.folderAssetsPath))
            {
                result = ConventionState.Valid;
            }
            return result;
        }

        public static void ProcessSubFolders(string path)
        {
            if (folderDictionary.ContainsKey(path))
            {
                List<JSONObject> allowedFileTypes = folderDictionary[path];
                List<FileData> assetList = GetAllFilesDataAtPath(path);
                if (allowedFileTypes != null && assetList.Count > 0)
                {
                    string localErrorMessage2 = string.Empty;
                    foreach (FileData asset in assetList)
                    {
                        ConventionState conventionState = CheckFileConvention(asset);
                        if (conventionState == ConventionState.NotValid)
                        {
                            localErrorMessage2 = localErrorMessage2 + "\n- " + asset.fullName + " does not meet the Convention criteria";
                        }
                    }
                    if (localErrorMessage2 != string.Empty)
                    {
                        string msg = localErrorMessage2;
                        localErrorMessage2 = "\nPath \"" + path + "\" issues:" + msg + "\n";
                        AddFolderError(localErrorMessage2);
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
                if (allowedFileTypes.Find((JSONObject x) => x.str == "folder") != null)
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
            else if (ignoreFolders.Contains(path))
            {
                AddFolderError("The path: " + path + " is not in the Convention File.");
            }
        }

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
            Object configFileData = AssetDatabase.LoadAssetAtPath("Assets/Gear/Config Files/ConventionKeeperConfig.json", typeof(Object));
            config.Clear();
            config = new JSONObject(configFileData.ToString());
            if (!config["active"])
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
                    folderDictionary.Add(folder["path"].str, folder["fileTypesAllowed"].list);
                }
            }

            foreach (JSONObject ignoredFolder in folderStructure["ignore"]["folders"].list)
            {
                ignoreFolders.Add(ignoredFolder.str);
            }

            ignoreFileTypes = new List<JSONObject>(folderStructure["ignore"]["fileTypes"].list);
            namingConvention = config["namingConvention"];
            fileTypes = new List<JSONObject>(namingConvention["fileTypes"].list);
            conventionKeyRules = new List<JSONObject>(namingConvention["conventionKeyRules"].list);
            foreach (JSONObject item in namingConvention["regexDictionary"].list)
            {
                regexDictionary.Add(item["function"].str, item["regex"].str);
            }
        }

        private static void AddFolderError(string error)
        {
            folderErrors = folderErrors + "\n" + error;
        }

        public static List<FileData> GetAllFilesDataAtPath(string path)
        {
            List<string> fileEntries = new List<string>(Directory.GetFiles(Application.dataPath + path.Remove(0, 6)));
            foreach (JSONObject type in ignoreFileTypes)
            {
                fileEntries.RemoveAll((string x) => x.Contains("." + type.str));
            }
            List<FileData> tmpFileDataList = new List<FileData>();
            foreach (string filePath in fileEntries)
            {
                tmpFileDataList.Add(new FileData(filePath));
            }
            return tmpFileDataList;
        }

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

        public static bool CheckSufixPrefix(string first, string second)
        {
            if ((first == "sufix" || first == "prefix") && (second == "sufix" || second == "prefix"))
            {
                return true;
            }
            return false;
        }
    }
}
