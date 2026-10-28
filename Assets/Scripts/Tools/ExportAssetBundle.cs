#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class CreateAssetBundles
{
    [MenuItem("Assets/Build Shader Bundle")]
    static void BuildAllAssetBundles()
    {
        // Bring up save panel
        string path = EditorUtility.SaveFilePanel("Save Resource", "", "New Resource", "unity3d");

        // Check if the user canceled the dialog
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("Asset bundle build canceled by user.");
            return;
        }

        // DeterministicAssetBundle was removed, builds are always deterministic now
        var opts = BuildAssetBundleOptions.ForceRebuildAssetBundle;
        BuildTarget[] platforms = { BuildTarget.StandaloneWindows, BuildTarget.StandaloneOSX, BuildTarget.StandaloneLinux64 };
        string[] platformExts = { "-windows", "-macosx", "-linux" };

        for (var i = 0; i < platforms.Length; ++i)
        {
            // Build the resource file from the active selection.
            Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
            var shaderAssets = new List<Object>();

            foreach (Object obj in selection)
            {
                string pathToObject = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(pathToObject))
                {
                    string[] shaderPaths = AssetDatabase.FindAssets("t:Shader", new[] { pathToObject });
                    foreach (string shaderGUID in shaderPaths)
                    {
                        string shaderPath = AssetDatabase.GUIDToAssetPath(shaderGUID);
                        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                        if (shader != null)
                        {
                            shaderAssets.Add(shader);
                        }
                    }

                    string[] computeShaderPaths = AssetDatabase.FindAssets("t:ComputeShader", new[] { pathToObject });
                    foreach (string shaderGUID in computeShaderPaths)
                    {
                        string shaderPath = AssetDatabase.GUIDToAssetPath(shaderGUID);
                        ComputeShader shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(shaderPath);
                        if (shader != null)
                        {
                            shaderAssets.Add(shader);
                        }
                    }
                }
                else if (obj is Shader || obj is ComputeShader)
                {
                    shaderAssets.Add(obj);
                }
            }

            // Selecting a folder also selects the shaders inside it, so remove duplicates
            string[] assetPaths = shaderAssets.Select(asset => AssetDatabase.GetAssetPath(asset)).Distinct().ToArray();

            BuildShaderBundle(assetPaths, path.Replace(".unity3d", platformExts[i] + ".unity3d"), opts, platforms[i]);
        }
    }

    // BuildPipeline.BuildAssetBundle was removed in Unity 6. Its replacement outputs a folder with manifests,
    // so build into a temp folder and copy the bundle out
    static void BuildShaderBundle(string[] assetPaths, string bundlePath, BuildAssetBundleOptions opts, BuildTarget platform)
    {
        string tempPath = FileUtil.GetUniqueTempPathInProject();
        Directory.CreateDirectory(tempPath);

        try
        {
            AssetBundleBuild build = new AssetBundleBuild
            {
                // Bundles loaded at the same time need unique names
                assetBundleName = Path.GetFileNameWithoutExtension(bundlePath),
                assetNames = assetPaths
            };

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(tempPath, new[] { build }, opts, platform);
            string[] builtBundles = manifest != null ? manifest.GetAllAssetBundles() : new string[0];
            if (builtBundles.Length == 0)
            {
                Debug.LogError("Failed to build shader bundle: " + bundlePath);
                return;
            }

            File.Copy(Path.Combine(tempPath, builtBundles[0]), bundlePath, true);
        }
        finally
        {
            FileUtil.DeleteFileOrDirectory(tempPath);
        }
    }
}
#endif
