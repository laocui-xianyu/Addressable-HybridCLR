using System.Collections.Generic;
using System.IO;
using HybridCLR.Editor;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class BuildTool : MonoBehaviour
    {
        [MenuItem("BuildTool/CopyDllsToTarget")]
        public static void CopyDllsToTarget()
        {
            List<string> assmblys = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
        
            string pathStart = ($"{Application.dataPath}/../HybridCLRData/HotUpdateDlls/{EditorUserBuildSettings.activeBuildTarget}");
            string pathEnd = ($"{Application.dataPath}/AddressableResources/HotUpdateDlls/");
        
            DirectoryInfo directoryInfo = new DirectoryInfo(pathStart);
            FileInfo[] files = directoryInfo.GetFiles();

            foreach (FileInfo file in files)
            {
                if (file.Extension == ".dll" && assmblys.Contains(file.Name.Substring(0, file.Name.Length - 4)))
                {
                    file.CopyTo(pathEnd+file.Name+".bytes", true);
                }
            }
        
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
