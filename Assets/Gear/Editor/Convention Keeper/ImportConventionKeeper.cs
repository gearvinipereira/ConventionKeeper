﻿using UnityEditor;
using UnityEngine;

namespace Gear.Tools.ConventionKeeper
{
    public class ImportConventionKeeper : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string importedAsset in importedAssets) 
            {
                ConventionState conventionState = ConventionKeeper.CheckImportFileConvention(importedAsset); 
                if (conventionState == ConventionState.NotValid)
                {
                    EditorUtility.DisplayDialog("Ops!", importedAsset + " is not a valid file.", "nhe");
                    //AssetDatabase.DeleteAsset(importedAsset);
                }
            }
            foreach (string movedAsset in movedAssets)
            {
                ConventionKeeper.CheckImportFileConvention(movedAsset);
            }
        }
    }
}