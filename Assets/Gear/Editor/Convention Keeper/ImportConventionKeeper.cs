using UnityEditor;
using UnityEngine;

namespace Gear.Tools.ConventionKeeper
{
    public class ImportConventionKeeper : AssetPostprocessor
    {
        //Unity function to intervene into the import pipeline and process the modified files
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            ConventionKeeper.CheckConventionKeeperState();

            if (ConventionKeeper.active)
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
}