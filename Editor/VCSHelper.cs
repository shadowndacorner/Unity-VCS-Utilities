/*
    The MIT License

    Copyright (c) 2018 Ian Diaz, https://shadowndacorner.com

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Reflection;
using Debug = UnityEngine.Debug;

public static class VCSHelper
{
    public static AbstractVCSHelper Instance
    {
        get;
        internal set;
    }

    static string _vcsHelperDirRoot;
    public static string VCSHelperDirRoot
    {
        get
        {
            if (_vcsHelperDirRoot == null)
            {
                foreach (var v in Directory.GetFiles("Assets", "VCSHelper.cs", SearchOption.AllDirectories))
                {
                    _vcsHelperDirRoot = Path.GetDirectoryName(v);
                }
            }
            return _vcsHelperDirRoot;
        }
    }

    public static string IconDirectory
    {
        get
        {
            return Path.Combine(VCSHelperDirRoot, "Icons");
        }
    }

    static Texture2D _localLockIcon;
    static Texture2D _remoteLockIcon;
    static Texture2D _modifiedIcon;
    public static Texture2D LocalLockIcon
    {
        get
        {
            if (!_localLockIcon)
            {
                _localLockIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(IconDirectory, "local-lock.png"));
            }
            return _localLockIcon;
        }
    }

    public static Texture2D RemoteLockIcon
    {
        get
        {
            if (!_remoteLockIcon)
            {
                _remoteLockIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(IconDirectory, "remote-lock.png"));
            }
            return _remoteLockIcon;
        }
    }

    public static Texture2D ModifiedItemIcon
    {
        get
        {
            if (!_modifiedIcon)
            {
                _modifiedIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(IconDirectory, "modified.png"));
            }
            return _modifiedIcon;
        }
    }

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        foreach(var type in typeof(VCSHelper).Assembly.GetExportedTypes())
        {
            var attrib = type.GetCustomAttributes(typeof(VCSImplementationAttribute), false);
            if (attrib.Length > 0 && typeof(AbstractVCSHelper).IsAssignableFrom(type))
            {
                var metadata = attrib[0] as VCSImplementationAttribute;
                var instance = System.Activator.CreateInstance(type) as AbstractVCSHelper;
                if (instance.IsActive)
                {
                    if (Instance == null)
                    {
                        instance.Initialize();
                        Instance = instance;
                        AbstractVCSHelper.SetMetadata(Instance, metadata);
                    }
                    else
                    {
                        Debug.LogError("[VCS] Found multiple valid VCS types: " + metadata.DisplayName + " and " + Instance.Metadata.DisplayName);
                        return;
                    }
                }
            }
        }
        EditorApplication.update += () =>
        {
            if (Instance != null)
            {
                Instance.Update();
            }
        };
    }

    [MenuItem("Assets/Version Control/Refresh Locks", false)]
    static void InitializeLocks()
    {
        Instance.RefreshLockedFiles();
    }

    [MenuItem("Assets/Version Control/Lock", false)]
    static void LockFileMenuItem()
    {
        Instance.LockFile();
    }

    [MenuItem("Assets/Version Control/Lock", true)]
    static bool validateLock()
    {
        return Instance.ContextMenuButtonEnabled(AbstractVCSHelper.ContextMenuButton.Lock);
    }

    [MenuItem("Assets/Version Control/Discard Changes", false, 0)]
    static void DiscardchangesMenuItem()
    {
        Instance.DiscardChanges();
    }

    [MenuItem("Assets/Version Control/Discard Changes", true)]
    static bool validateDiscardchangesMenuItem()
    {
        return Instance.ContextMenuButtonEnabled(AbstractVCSHelper.ContextMenuButton.DiscardChanges);
    }

    [MenuItem("Assets/Version Control/Unlock", false)]
    static void UnlockFileMenuItem()
    {
        Instance.UnlockFile();
    }

    [MenuItem("Assets/Version Control/Unlock", true)]
    static bool validateUnlock()
    {
        return Instance.ContextMenuButtonEnabled(AbstractVCSHelper.ContextMenuButton.Unlock);
    }

    public static void AssetModified()
    {
        Instance.HandleModifiedAsset();
    }

    // TODO: Improve
    [MenuItem("Version Control/Git Utilities/Resolve SmartMerge Conflicts")]
    public static void ResolveMergeConflicts()
    {
        var startInfo = new ProcessStartInfo();
        startInfo.FileName = "git";
        startInfo.Arguments = "mergetool --tool=unityyamlmerge";
        startInfo.UseShellExecute = true;
        var proc = Process.Start(startInfo);
    }
}

public class VCSConfigWindow : EditorWindow
{
    [MenuItem("Version Control/VCS Menu", priority = 0)]
    public static VCSConfigWindow OpenWindow()
    {
        return GetWindow<VCSConfigWindow>("VCS Menu");
    }

    Vector2 scroll;

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (VCSHelper.Instance != null)
        {
            EditorGUILayout.LabelField("Detected " + VCSHelper.Instance.Metadata.DisplayName);
            VCSHelper.Instance.ConfigMenuOnGui();
        }
        else
        {
            EditorGUILayout.LabelField("No supported VCS detected");
        }
        EditorGUILayout.EndScrollView();
    }
}