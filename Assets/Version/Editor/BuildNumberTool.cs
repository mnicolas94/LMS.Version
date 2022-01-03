﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace LMS.Version
{
    public class BuildNumberTool : IPreprocessBuildWithReport
    {
        public int callbackOrder
        {
            get { return 0; }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            var versionAsset = GetVersionAsset();
            
            // get version tag
            try
            {
                var (major, minor, patch) = GetCurrentVersion();
                versionAsset.GameVersion.Major = major;
                versionAsset.GameVersion.Minor = minor;
                versionAsset.GameVersion.Build = patch;
            }
            catch (Exception e)
            {
                throw new BuildFailedException($"Could not get version: {e}");
            }
            
            versionAsset.Initialize();

            // Push the version number into Unity's version field. Some console platforms really care about this!
            // (for example, xbox games can fail cert if the version number isnt changed here)
            PlayerSettings.bundleVersion = versionAsset.GameVersion.ToString();
            PlayerSettings.macOS.buildNumber = versionAsset.GameVersion.ToString();

            // get hash
            try
            {
                string gitHash = GitUtils.GetGitCommitHash();
                versionAsset.GitHash = gitHash;
            }
            catch (Exception e)
            {
                string errorMessage = $"Could not get commit hash: {e}";
                throw new BuildFailedException(errorMessage);
            }

            versionAsset.BuildTimestamp = DateTime.UtcNow.ToString("yyyy MMMM dd - HH:mm");

            // Save changes
            EditorUtility.SetDirty(versionAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"BuildNumberTool.OnPreprocessBuild: " +
                      $"Storing version: {versionAsset.GameVersion}, " +
                      $"commit hash: {versionAsset.GitHash} and " +
                      $"timestamp: {versionAsset.BuildTimestamp}");
        }
        
        private static Version GetVersionAsset(){
            var assets = AssetDatabase.FindAssets($"t:{typeof(Version).Name}");
            Version versionAsset = null;

            if (assets.Length == 0)
            {
                versionAsset = VersionSettingProvider.GenerateNewVersionAsset();
            }
            else if (assets.Length > 1)
            {
                throw new BuildFailedException(
                    $"More than one Version asset in the project. Please ensure only one exists.\n{string.Join("\n", assets.Select(s => AssetDatabase.GUIDToAssetPath(s)))}");
            }
            else if (assets.Length == 1)
            {
                versionAsset = AssetDatabase.LoadAssetAtPath<Version>(AssetDatabase.GUIDToAssetPath(assets[0]));
            }

            return versionAsset;
        }

        private static (int, int, int) GetCurrentVersion()
        {
            var lastTag = GitUtils.GetLastTag();
            var trimmedTag = lastTag.TrimStart('v');
            var tokens = trimmedTag.Split('.');
            try
            {
                int major = Int32.Parse(tokens[0]);
                int minor = Int32.Parse(tokens[1]);
                string strPatch = tokens[2];
                int patch;
                if (strPatch.Contains('-'))
                {
                    var patchTokens = tokens[2].Split('-');
                    patch = Int32.Parse(patchTokens[0]);
                }
                else
                {
                    patch = Int32.Parse(tokens[2]);
                }
                
                return (major, minor, patch);
            }
            catch (Exception e)
            {
                throw new BuildFailedException($"Error parsing git tag: {lastTag}");
            }
        }
    }
}
