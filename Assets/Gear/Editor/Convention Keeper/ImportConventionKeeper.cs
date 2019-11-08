using UnityEditor;
using UnityEngine;

namespace Gear.Tools.ConventionKeeper
{
    public class ImportConventionKeeper : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string importedAsset in importedAssets) 
            {
                FileConventionState conventionState = ConventionKeeper.CheckImportFileConvention(importedAsset);
            }
            foreach (string movedAsset in movedAssets)
            {
                ConventionKeeper.CheckImportFileConvention(movedAsset);
            }
        }
    }
}