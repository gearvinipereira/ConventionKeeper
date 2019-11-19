using UnityEditor;
using UnityEngine;

namespace Gear.Tools.ConventionKeeper
{
    /// <summary>
    /// Unity class wraper for validating files when imported.
    /// </summary>
    public class ImportConventionKeeper : AssetPostprocessor
    {
        /// <summary>
        /// Unity function to intervene into the import pipeline and process the modified files
        /// </summary>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            //Check if can run the auto catch on a minimum setup level
            if (EditorPrefs.HasKey(ConventionKeeper.setupDoneKey) && !ConventionKeeper.usingOverview)
            {
                //Check if can run the auto catch on a post setup level
                if (ConventionKeeper.active && EditorPrefs.GetBool(ConventionKeeper.setupDoneKey))
                {
                    //Process all imported assets
                    foreach (string importedAsset in importedAssets)
                    {
                        FileConventionState conventionState = ConventionKeeper.CheckImportFileConvention(importedAsset);
                    }

                    //Process all moved assets
                    foreach (string movedAsset in movedAssets)
                    {
                        ConventionKeeper.CheckImportFileConvention(movedAsset);
                    }
                }
            }
        }
    }
}