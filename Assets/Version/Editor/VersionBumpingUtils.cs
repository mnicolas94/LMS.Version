﻿using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using Version = LMS.Version.Version;

namespace LMS.Version
{
    public static class VersionBumpingUtils
    {
        private static Version GetVersionAsset()
        {
            // Find the version asset and increment its version number
            var assets = AssetDatabase.FindAssets($"t:{typeof(Version).Name}");
            Version versionAsset = null;

            if (assets.Length == 0)
            {
                throw new Exception("Version asset not found");
            }
            if (assets.Length > 1)
            {
                throw new Exception(
                    $"More than one Version asset in the project." +
                    $" Please ensure only one exists.\n" +
                    $"{string.Join("\n", assets.Select(s => AssetDatabase.GUIDToAssetPath(s)))}");
            }
            if (assets.Length == 1)
            {
                versionAsset = AssetDatabase.LoadAssetAtPath<Version>(AssetDatabase.GUIDToAssetPath(assets[0]));
            }

            return versionAsset;
        }

        public static void BumpMajorVersion()
        {
            var version = GetVersionAsset();
            version.GameVersion.Major++;
            version.GameVersion.Minor = 0;
            version.GameVersion.Build = 0;
            SaveVersionAsset(version);
        }
        
        public static void BumpMinorVersion()
        {
            var version = GetVersionAsset();
            version.GameVersion.Minor++;
            version.GameVersion.Build = 0;
            SaveVersionAsset(version);
        }
        
        public static void BumpPatchVersion()
        {
            var version = GetVersionAsset();
            version.GameVersion.Build++;
            SaveVersionAsset(version);
        }
        
        public static void UpdateHashAndTimeStamp()
        {
            var version = GetVersionAsset();
            string gitHash = GitUtils.GetGitCommitHash();
            version.GitHash = gitHash;
            version.BuildTimestamp = DateTime.UtcNow.ToString("yyyy MMMM dd - HH:mm");
            SaveVersionAsset(version);
        }

        private static void SaveVersionAsset(Version versionAsset)
        {
            PlayerSettings.bundleVersion = versionAsset.GameVersion.ToString();
            PlayerSettings.macOS.buildNumber = versionAsset.GameVersion.ToString();
            EditorUtility.SetDirty(versionAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}