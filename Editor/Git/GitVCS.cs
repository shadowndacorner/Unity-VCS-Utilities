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

using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;

using System.Text;
using Debug = UnityEngine.Debug;
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;

[VCSImplementation(DisplayName = "Git")]
public class GitVCS : AbstractVCSHelper
{
    [System.Serializable]
    struct LockFileStorage
    {
        public int PID;
        public List<LockedFile> Locks;
    }

    // This is mostly to clean up the class
    class GitOnGui
    {
        GUIContent ConfigLabel = new GUIContent("Configuration");
        GUIContent LockedFileLabel = new GUIContent("Locked Files");
        GUIContent ModifiedFilesLabel = new GUIContent("Modified Files");
        bool configFoldout;
        bool lockedFilesFoldout;
        bool modifiedFilesFoldout;

        bool hasCheckedVersion;
        bool versionWorks;

        void OpenGitForWindowsDLPage()
        {
            //https://github.com/git-for-windows/git/releases
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "start";
            startInfo.Arguments = "https://github.com/git-for-windows/git/releases";
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = true;
            Process.Start(startInfo);
        }

        public void Draw(GitVCS vcs)
        {
            if ((configFoldout = EditorGUILayout.Foldout(configFoldout, ConfigLabel)))
            {
                ++EditorGUI.indentLevel;
                // Username
                {
                    string value = VCSPrefs.HasKey(GitHelper.VCSPrefsKeys.UsernamePrefKey) ? VCSPrefs.GetString(GitHelper.VCSPrefsKeys.UsernamePrefKey) : "";
                    string newvalue = EditorGUILayout.TextField("Github Username", value);
                    if (newvalue != value)
                    {
                        VCSPrefs.SetString(GitHelper.VCSPrefsKeys.UsernamePrefKey, newvalue);
                    }
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Auto Update Submodules");
                GitHelper.AutoUpdateSubmodules = EditorGUILayout.Toggle(GitHelper.AutoUpdateSubmodules, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("LFS Enabled");
                GitHelper.LFSEnabled = EditorGUILayout.Toggle(GitHelper.LFSEnabled, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                if (GitHelper.LFSEnabled)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Automatically Lock Scenes on Save");
                    GitHelper.SceneAutoLock = EditorGUILayout.Toggle(GitHelper.SceneAutoLock, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Remote Lock Prevents Edits");
                    GitHelper.PreventEditsOnRemoteLock = EditorGUILayout.Toggle(GitHelper.PreventEditsOnRemoteLock, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();
                }

                if (!hasCheckedVersion)
                {
                    if (GUILayout.Button("Check Git Compatibility"))
                    {
                        hasCheckedVersion = true;
                        GitHelper.RunGitCommand("version",
                            (proc) =>
                            {
                                if (!proc.WaitForExit(2000))
                                {
                                    return true;
                                }
                                return false;
                            },
                            result =>
                            {
                                // We know what version is required on Windows, must check
                                // later for OSX/Linux.
                                if (result.Contains("windows"))
                                {
                                    result = result.Replace("git version ", "");

                                    var split = result.Trim().Split('.');
                                    var majorVersion = int.Parse(split[0].Trim());
                                    var minorVersion = int.Parse(split[1].Trim());
                                    if (majorVersion > 2 || (majorVersion == 2 && minorVersion >= 16))
                                    {
                                        versionWorks = true;
                                    }
                                    else
                                    {
                                        versionWorks = false;
                                        if (EditorUtility.DisplayDialog("Version Control", "Git for Windows out of date, this plugin requires at least version 2.16.  Would you like to open the GitHub Releases page in your web browser?", "Yes", "No"))
                                        {
                                            OpenGitForWindowsDLPage();
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.Log("Version checking only supported on Windows.  If the plugin doesn't work, ");
                                    Debug.Log("Git Version: " + result);
                                }
                            },
                            error =>
                            {
                                Debug.LogError(error);
                                return true;
                            }
                        );
                    }
                }
                else
                {
                    if (Application.platform != RuntimePlatform.WindowsEditor)
                    {
                        EditorGUILayout.LabelField("Check Console, version checking only functional on Windows");
                    }
                    else if (versionWorks)
                    {
                        EditorGUILayout.LabelField("Local git compatibility good!");
                    }
                    else
                    {
                        if (GUILayout.Button("Update Git for Windows (opens GitHub Releases page)"))
                        {
                            OpenGitForWindowsDLPage();
                        }
                    }
                }
                --EditorGUI.indentLevel;
            }

            if ((lockedFilesFoldout = EditorGUILayout.Foldout(lockedFilesFoldout, LockedFileLabel)))
            {
                ++EditorGUI.indentLevel;
                foreach (var v in vcs.LockedFiles)
                {
                    GUILayout.Label(v.Key + ": Locked by " + v.Value.User);
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Select"))
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(v.Value.Path);
                    }

                    if (vcs.IsFileLockedByLocalUser(v.Key))
                    {
                        if (GUILayout.Button("Unlock"))
                        {
                            try
                            {
                                vcs.GitUnlockFile(new string[] { v.Key });
                            }
                            catch (System.Exception ex)
                            {
                                // Do nothing, it'll tell them
                            }
                            break;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
                --EditorGUI.indentLevel;
            }

            if ((modifiedFilesFoldout = EditorGUILayout.Foldout(modifiedFilesFoldout, ModifiedFilesLabel)))
            {
                ++EditorGUI.indentLevel;
                foreach (var v in vcs.ModifiedPaths)
                {
                    if (!File.Exists(v))
                        continue;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(v);
                    if (GUILayout.Button("Select", GUILayout.ExpandWidth(false)))
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(v);
                    }

                    if (GUILayout.Button("Discard Changes", GUILayout.ExpandWidth(false)))
                    {
                        var oldSelection = Selection.activeObject;
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(v);
                        vcs.DiscardChanges();
                        Selection.activeObject = oldSelection;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                }
                --EditorGUI.indentLevel;
            }

            if (GUILayout.Button("Refresh Locks"))
            {
                vcs.RefreshGitLockTypes();
                vcs.RefreshGitLocks();
            }

            if (GUILayout.Button("Reset Configuration"))
            {
                GitHelper.VCSPrefsKeys.ResetConfig();
            }
        }
    }

    [System.Serializable]
    public class LockedFile
    {
        public string Path;
        public string User;
        public FileStream FileLock;
    }

    public HashSet<string> LockableExtensions = new HashSet<string> { };
    public HashSet<string> ModifiedPaths = new HashSet<string>();

    const string LockedFileKey = "git-vcs-lockedfiles-key";
    Dictionary<string, LockedFile> _lockedFiles;
    public Dictionary<string, LockedFile> LockedFiles
    {
        get
        {
            if (_lockedFiles == null)
            {
                LoadLockedFilesFromVCSPrefs();
            }
            return _lockedFiles;
        }
    }

    void UpdateLockedFiles()
    {
        if (!GitHelper.LFSEnabled)
        {
            return;
        }

        var storage = new LockFileStorage();
        storage.PID = Process.GetCurrentProcess().Id;

        var list = new List<LockedFile>();
        foreach (var v in LockedFiles)
        {
            list.Add(v.Value);
        }
        storage.Locks = list;
        VCSPrefs.SetString(LockedFileKey, JsonUtility.ToJson(storage));
    }

    void LoadLockedFilesFromVCSPrefs(bool forceRebuild = false)
    {
        if (!GitHelper.LFSEnabled)
            return;

        if (_lockedFiles != null)
        {
            foreach (var v in _lockedFiles)
            {
                if (v.Value.FileLock != null)
                {
                    v.Value.FileLock.Dispose();
                }
            }
        }

        _lockedFiles = new Dictionary<string, LockedFile>();
        if (VCSPrefs.HasKey(LockedFileKey))
        {
            var storage = JsonUtility.FromJson<LockFileStorage>(VCSPrefs.GetString(LockedFileKey));

            // If this is a different run, let's refresh git locks
            if ((storage.PID != Process.GetCurrentProcess().Id) || forceRebuild)
            {
                RefreshGitLocks();
            }
            else
            {
                // Otherwise, this is just an assembly load and we can probably use the old locks
                foreach (var v in storage.Locks)
                {
                    _lockedFiles.Add(v.Path, v);
                    if (v.User != GitHelper.Username)
                    {
                        if (File.Exists(v.Path))
                        {
                            try
                            {
                                v.FileLock = new FileStream(v.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError("Failed to create file lock for " + v.Path + ": " + ex);
                            }
                        }
                    }
                }
            }
        }
    }

    public override bool IsActive
    {
        get
        {
            return FindGitRoot();
        }
    }

    public static string GitRoot
    {
        get;
        internal set;
    }

    public static string RelativePathToUnityDirectory
    {
        get;
        internal set;
    }

    public static bool GitRootIsUnityRoot
    {
        get;
        internal set;
    }

    static bool FindGitRoot()
    {
        if (GitRoot != null)
            return true;

        bool found = false;
        GitHelper.RunGitCommand("rev-parse --show-toplevel",
            proc =>
            {
                if (!proc.WaitForExit(5000))
                {
                    return true;
                }
                return false;
            },

            result =>
            {
                GitRoot = result.Replace('\\', '/');
                found = true;
            },

            error =>
            {
                if (error.Contains("fatal"))
                {
                    Debug.LogError("[Git VCS Init] " + error);
                    return true;
                }
                return false;
            }
        );

        if (found)
        {
            RelativePathToUnityDirectory = System.Environment.CurrentDirectory.Replace('\\', '/').Replace(GitRoot, "");
            if (RelativePathToUnityDirectory.StartsWith("/"))
                RelativePathToUnityDirectory = RelativePathToUnityDirectory.Substring(1);
            GitRootIsUnityRoot = string.IsNullOrEmpty(RelativePathToUnityDirectory) || RelativePathToUnityDirectory == "/" || RelativePathToUnityDirectory == "." || RelativePathToUnityDirectory == "./";
        }

        return found;
    }

    bool inPlayMode = false;
    public override void Initialize()
    {
        if (!FindGitRoot())
        {
            Debug.LogError("[VCS] Unable to find .git folder, git support disabled");
            return;
        }

        Thread m_asyncthread = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    if (inPlayMode)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    UpdateModifiedFilesAsync();

                    var newLockFiles = new Dictionary<string, LockedFile>();
                    if (GitHelper.LFSEnabled)
                    {
                        // Update locks
                        GitHelper.RunGitCommand("lfs locks",
                            proc =>
                            {
                                if (!proc.WaitForExit(5000))
                                {
                                    return true;
                                }
                                return false;
                            },

                            result =>
                            {
                                try
                                {
                                    var parts = result.Split('\t');
                                    var path = GitHelper.GitToUnityPath(parts[0]);
                                    var user = parts[1];

                                    var locked = new LockedFile();
                                    locked.Path = path;
                                    locked.User = user;
                                    newLockFiles.Add(path.Trim(), locked);
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError("[VCS Async] " + ex);
                                }
                            },

                            error =>
                            {
                                Debug.LogError("[VCS]" + error);
                                return true;
                            }
                        );

                        lock (_actionQueueLock)
                        {
                            var cnt = LockedFiles.Count;
                            string hashStr = "";
                            foreach (var v in LockedFiles)
                                hashStr += v.Key + ": " + v.Value.User.GetHashCode();

                            int hash = hashStr.GetHashCode();
                            _toRunOnMainThread.Enqueue(() =>
                            {
                            // Locked files
                            if (cnt == LockedFiles.Count)
                                {
                                    var newHashStr = "";
                                    foreach (var v in LockedFiles)
                                        newHashStr += v.Key + ": " + v.Value.User.GetHashCode();

                                    int newhash = newHashStr.GetHashCode();

                                // Let's make sure that the super lazy hashes match
                                if (newhash == hash)
                                    {
                                        foreach (var v in LockedFiles)
                                        {
                                            if (v.Value.FileLock != null)
                                                v.Value.FileLock.Dispose();
                                        }

                                        LockedFiles.Clear();
                                        _lockedFiles = newLockFiles;
                                        foreach (var v in LockedFiles)
                                        {
                                            if (v.Value.User != GitHelper.Username && GitHelper.PreventEditsOnRemoteLock)
                                            {
                                                if (File.Exists(v.Value.Path))
                                                {
                                                    try
                                                    {
                                                        v.Value.FileLock = new FileStream(v.Value.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                                                    }
                                                    catch (System.Exception ex)
                                                    {
                                                        Debug.LogError("Failed to create file lock for " + v.Key + ": " + ex);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                UpdateLockedFiles();
                            });
                        }
                    }
                    Thread.Sleep(10000);
                }
                catch (ThreadAbortException ex)
                {
                    throw ex;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[VCS Async] " + ex);
                }
            }
        });

        m_asyncthread.Name = "Git Async Thread";
        m_asyncthread.Start();

        // Preserves locked files for play mode, etc
        AssemblyReloadEvents.beforeAssemblyReload += () =>
        {
            foreach(var v in LockedFiles)
            {
                if (v.Value.FileLock != null)
                {
                    v.Value.FileLock.Dispose();
                }
            }
            m_asyncthread.Abort();
            UpdateLockedFiles();
        };

        UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += (scn) =>
        {
            if (GitHelper.SceneAutoLock)
            {
                if (string.IsNullOrEmpty(scn.path))
                    return;

                if (!IsFileLockedByLocalUser(scn.path))
                {
                    GitLockFile(new string[] { scn.path });
                }
            }
        };

        EditorApplication.playModeStateChanged += (s) =>
        {
            if (s == PlayModeStateChange.ExitingEditMode)
            {
                UpdateLockedFiles();
            }
        };

        EditorApplication.projectWindowItemOnGUI +=
            (string guid, Rect rect) =>
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var iconRect = rect;
                var oldBack = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0, 0, 0, 0);

                if (ModifiedPaths.Contains(path))
                {
                    GUI.Box(iconRect, VCSHelper.ModifiedItemIcon);
                }

                if (IsFileLockedByLocalUser(path))
                {
                    GUI.Box(iconRect, new GUIContent(VCSHelper.LocalLockIcon, "Locked by " + LockedFiles[path].User + " (local user)"));
                }
                else if (IsFileLocked(path))
                {
                    GUI.Box(iconRect, new GUIContent(VCSHelper.RemoteLockIcon, "Locked by " + LockedFiles[path].User));
                }


                GUI.backgroundColor = oldBack;
            };

        if (VCSPrefs.HasInitializedKeys && !GitHelper.Configured)
        {
            if (EditorUtility.DisplayDialog("Version Control", "You have not yet set up your GitHub username and you will not be able to lock files.  Would you like to open the configuration window?", "Yes", "No"))
            {
                VCSConfigWindow.OpenWindow();
            }
        }
        else
        {
            LoadLockedFilesFromVCSPrefs();
            RefreshGitLockTypes();
        }
    }

    // This way we can refresh locks asynchronously
    object _actionQueueLock = new object();
    Queue<System.Action> _toRunOnMainThread = new Queue<System.Action>();

    public override void Update()
    {
        inPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;
        if (_toRunOnMainThread.Count > 0)
        {
            lock (_actionQueueLock)
            {
                // This should be minimal overhead, so we'll only run one per frame
                if (_toRunOnMainThread.Count > 0)
                {
                    try
                    {
                        _toRunOnMainThread.Dequeue()();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("[VCS Sync] " + ex);
                    }
                }
            }
        }
    }

    public string GetCurrentBranch()
    {
        string branch = null;
        GitHelper.RunGitCommand("branch",
            (proc) =>
            {
                if (!proc.WaitForExit(5000))
                    return true;
                return false;
            },
            result =>
            {
                var split = result.Trim().Split(' ');
                if (split.Length > 0 && split[0].Trim() == "*")
                {
                    branch = split[split.Length - 1].Trim();
                }
            },
            error =>
            {
                Debug.LogError("[VCS Branch] " + error);
                return true;
            }
        );
        return branch;
    }

    public override List<string> GetChangedFiles()
    {
        List<string> changed = new List<string>();

        // TODO: Handle when git directory is not the project root
        GitHelper.RunGitCommand("diff --name-only",
            proc =>
            {
                if (!proc.WaitForExit(5000))
                    return true;
                return false;
            },
            result =>
            {
                var unityPath = GitHelper.GitToUnityPath(result);
                if (File.Exists(unityPath) || Directory.Exists(unityPath))
                {
                    changed.Add(unityPath);
                }
            },
            error =>
            {
                if (error.Contains("warning") || error.Contains("line endings"))
                    return false;

                Debug.LogError("[VCS Diff] " + error);
                return true;
            }
        );

        return changed;
    }

    public override List<string> GetTrackedFiles()
    {
        List<string> tracked = new List<string>();

        // TODO: Handle when git directory is not the project root
        GitHelper.RunGitCommand("ls-tree -r " + GetCurrentBranch() + " --name-only",
            proc =>
            {
                if (!proc.WaitForExit(5000))
                    return true;
                return false;
            },
            result =>
            {
                if (File.Exists(result))
                {
                    tracked.Add(GitHelper.GitToUnityPath(result));
                }
            },
            error =>
            {
                if (error.Contains("warning"))
                    return false;

                Debug.LogError("[VCS Tracked] " + error);
                return true;
            }
        );

        return tracked;
    }

    public override void DiscardChanges()
    {
        var guids = Selection.assetGUIDs;
        var paths = new List<string>(guids.Length);
        var changed = new HashSet<string>(GenerateRecursiveModifiedList());
        foreach (var v in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(v).Replace('\\', '/');
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    var filepath = file.Replace('\\', '/');
                    if (changed.Contains(filepath))
                    {
                        paths.Add(filepath);
                    }
                }
            }
            else
            {
                if (changed.Contains(path))
                    paths.Add(path);
            }
        }

        if (paths.Count == 0)
        {
            EditorUtility.DisplayDialog("Version Control", "No changes to discard", "Okay");
            return;
        }

        var msg = new StringBuilder();
        msg.AppendLine("Are you sure you want to discard changes?  The following files will be reverted:\n");
        foreach (var v in paths)
            msg.AppendLine(v);

        if (EditorUtility.DisplayDialog("Version Control", msg.ToString(), "Yes", "No"))
        {
            var arguments = new StringBuilder();
            foreach (var v in paths)
            {
                arguments.Append('"' + v + "\" ");
            }
            GitRevertFile(paths.ToArray());
            AssetDatabase.Refresh();
        }
    }

    public override bool LockFile()
    {
        var path = ActiveTargetPath;
        if (File.Exists(path))
        {
            return GitLockFile(new string[] { path });
        }
        return false;
    }

    public override bool UnlockFile()
    {
        var path = ActiveTargetPath;
        if (File.Exists(path))
        {
            return GitUnlockFile(new string[] { path });
        }
        return false;
    }

    public override bool IsFileLockable(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        if (string.IsNullOrEmpty(ext))
            return false;

        return (LockableExtensions.Contains(Path.GetExtension(ext)));
    }

    public override bool IsFileLocked(string path)
    {
        return LockedFiles.ContainsKey(path);
    }

    public override bool IsFileLockedByLocalUser(string path)
    {
        return IsFileLocked(path) && LockedFiles[path].User == GitHelper.Username;
    }

    public override void RefreshLockedFiles()
    {
        RefreshGitLockTypes();
        LoadLockedFilesFromVCSPrefs(true);
    }

    GitOnGui _inst;
    public override void ConfigMenuOnGui()
    {
        // This is just to clean up the namespace
        if (_inst == null)
            _inst = new GitOnGui();

        _inst.Draw(this);
    }

    public override bool ContextMenuButtonEnabled(ContextMenuButton button)
    {
        switch (button)
        {
            case ContextMenuButton.Lock:
                return TargetPaths.Length == 1 && IsFileLockable(ActiveTargetPath) && !IsFileLocked(ActiveTargetPath);
            case ContextMenuButton.Unlock:
                return TargetPaths.Length == 1 && IsFileLockedByLocalUser(ActiveTargetPath);
            case ContextMenuButton.DiscardChanges:
                return true;
        }
        return false;
    }

    public override void HandleModifiedAsset()
    {
        UpdateModifiedFilesAsync();
    }

    /// <summary>
    /// Locks a set of files in git.
    /// </summary>
    /// <param name="paths"></param>
    /// <returns></returns>
    public bool GitLockFile(string[] paths)
    {
        if (!GitHelper.LFSEnabled)
            return false;

        var cmdstring = new StringBuilder();
        foreach (var path in paths)
        {
            cmdstring.Append('"' + path + '"');
        }

        /*
        GitHelper.RunGitCommand("track -- " + cmdstring,
            proc =>
            {
                if (!proc.WaitForExit(5000))
                {
                    return true;
                }
                return false;
            }
        );*/

        bool hasError = GitHelper.RunGitCommand("lfs lock -- " + cmdstring,
            proc =>
            {
                try
                {
                    while (!proc.HasExited)
                    {
                        if (paths.Length > 1)
                        {
                            if (EditorUtility.DisplayCancelableProgressBar("Version Control", "Locking files " + (cmdstring.ToString()) + "...", 0))
                            {
                                proc.Kill();
                                return true;
                            }
                        }
                        else
                        {
                            if (EditorUtility.DisplayCancelableProgressBar("Version Control", "Locking file " + Path.GetFileName(paths[0]) + "...", 0))
                            {
                                proc.Kill();
                                return true;
                            }
                        }
                        Thread.Sleep(16);
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
                return false;
            },
            result =>
            {
                Debug.Log("[LFS Lock] " + result);
                if (!result.Contains("Locked"))
                {
                    // Failed for some reason and didn't go to std::error, search for updated locks.
                    RefreshGitLocks();
                }
                else
                {
                    foreach (var path in paths)
                    {
                        var locked = new LockedFile();
                        locked.Path = GitHelper.GitToUnityPath(path);
                        locked.User = GitHelper.Username;
                        LockedFiles.Add(path.Trim(), locked);
                    }
                }
            },
            error =>
            {
                Debug.Log("Failed to lock " + cmdstring);
                Debug.LogError(error);
                return true;
            });

        EditorApplication.RepaintProjectWindow();
        UpdateLockedFiles();
        return !hasError;
    }

    public bool GitUnlockFile(string[] paths)
    {
        if (!GitHelper.LFSEnabled)
            return false;

        var cmdstring = new StringBuilder();
        foreach (var path in paths)
        {
            cmdstring.Append('"' + path + '"');
        }

        bool hasError = GitHelper.RunGitCommand("lfs unlock -- " + cmdstring.ToString(),
            proc =>
            {
                try
                {
                    while (!proc.HasExited)
                    {
                        if (paths.Length > 1)
                        {
                            if (EditorUtility.DisplayCancelableProgressBar("Version Control", "Unlocking files " + (cmdstring.ToString()) + "...", 0))
                            {
                                proc.Kill();
                                return true;
                            }
                        }
                        else
                        {
                            if (EditorUtility.DisplayCancelableProgressBar("Version Control", "Unlocking file " + Path.GetFileName(paths[0]) + "...", 0))
                            {
                                proc.Kill();
                                return true;
                            }
                        }
                        Thread.Sleep(16);
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
                return false;
            },
            result =>
            {
                Debug.Log("[LFS Unlock] " + result);
                if (!result.Contains("Unlocked"))
                {
                    // Failed for some reason and didn't go to std::error, search for updated locks.
                    RefreshGitLocks();
                }
                else
                {
                    foreach (var path in paths)
                    {
                        LockedFiles.Remove(path.Trim());
                    }
                }
            },
            error =>
            {
                EditorUtility.DisplayDialog("Version Control", "Error while unlocking file: " + error, "Okay");

                // If it's erroring because it's already locked with local changes, ignore.
                // Otherwise, someone else locked the file before we did so everything is confused
                if (!error.Contains("uncommitted"))
                {
                    RefreshGitLocks();
                }

                return true;
            }
        );

        EditorApplication.RepaintProjectWindow();
        UpdateLockedFiles();
        return !hasError;
    }

    public void GitRevertFile(string[] paths)
    {
        // Yes this generates a lot of garbage but it's fine
        var rPath = new List<string>();
        var tracked = new HashSet<string>(GetChangedFiles());

        foreach (var v in paths)
        {
            if (tracked.Contains(v))
            {
                rPath.Add(v);
            }
            else
            {
                if (File.Exists(v))
                {
                    File.Delete(v);
                }
            }
        }

        if (rPath.Count == 0)
        {
            //Debug.Log("[VCS Discard] No git reverts necessary");
            return;
        }
        var cmdstring = new StringBuilder();
        foreach (var path in rPath)
        {
            cmdstring.Append('"' + path + '"');
        }

        GitHelper.RunGitCommand("checkout -- " + cmdstring.ToString(),
            (proc) =>
            {
                if (!proc.WaitForExit(2000))
                {
                    return true;
                }
                return false;
            },
            result =>
            {
                Debug.Log("[VCS Discard] " + result);
            },
            error =>
            {
                Debug.LogError(error);
                return true;
            });
        UpdateLockedFiles();
    }

    public void RefreshGitLocks()
    {
        if (GitHelper.AutoUpdateSubmodules)
        {
            try
            {
                var startinfo = new ProcessStartInfo();
                startinfo.FileName = "git";
                startinfo.RedirectStandardOutput = true;
                startinfo.RedirectStandardError = true;
                startinfo.UseShellExecute = false;
                startinfo.CreateNoWindow = true;
                startinfo.Arguments = "submodule update --init --recursive";

                var proc = Process.Start(startinfo);
                string line = "Downloading...";
                while (!proc.HasExited)
                {
                    while (!proc.StandardError.EndOfStream && (line = proc.StandardError.ReadLine()) != null)
                    {
                        Debug.LogError(line);
                    }

                    while (!proc.StandardOutput.EndOfStream && (line = proc.StandardOutput.ReadLine()) != null)
                    {
                        Debug.Log(line);
                    }

                    EditorUtility.DisplayProgressBar("Downloading submodules...", line, 0);
                    Thread.Sleep(30);
                }
                Debug.Log("Submodules completed");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.Refresh();
        }

        GitHelper.RunGitCommand("lfs",
            proc=>
            {
                // If it doesn't work in 10 seconds there's something wrong
                proc.WaitForExit(10000);
                return false;
            },
            result =>
            {

            },
            error =>
            {
                if (error.Contains("'lfs' is "))
                {
                    EditorUtility.DisplayDialog("Version Control", "Error: Git LFS is not installed.  File locking will not work.  Please install Git LFS.", "Okay");
                    GitHelper.LFSEnabled = false;
                }
                return true;
            });

        if (!GitHelper.LFSEnabled)
            return;

        foreach (var v in LockedFiles)
        {
            if (v.Value.FileLock != null)
            {
                v.Value.FileLock.Dispose();
            }
        }

        LockedFiles.Clear();

        if (GitHelper.LFSEnabled)
        {
            GitHelper.RunGitCommand("lfs locks",
                proc =>
                {
                    try
                    {
                        while (!proc.HasExited)
                        {
                            if (EditorUtility.DisplayCancelableProgressBar("Version Control", "Refreshing LFS locks...", 0))
                            {
                                proc.Kill();
                                return true;
                            }
                            Thread.Sleep(16);
                        }
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                    return false;
                },

                result =>
                {
                    var parts = result.Split('\t');
                    var path = GitHelper.GitToUnityPath(parts[0]);
                    Debug.Log("Locking path " + path + "(from " + parts[0] + ")");
                    var user = parts[1];

                    var locked = new LockedFile();
                    locked.Path = path;
                    locked.User = user;

                    if (user != GitHelper.Username && GitHelper.PreventEditsOnRemoteLock)
                    {
                        if (File.Exists(path))
                        {
                            try
                            {
                                locked.FileLock = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError("Failed to create file lock for " + path + ": " + ex);
                            }
                        }
                    }
                    LockedFiles.Add(path.Trim(), locked);
                },

                error =>
                {
                    Debug.LogError(error);
                    return true;
                }
            );
        }

        UpdateLockedFiles();
        EditorApplication.RepaintProjectWindow();
    }

    public void RefreshGitLockTypes()
    {
        LockableExtensions.Clear();
        GitHelper.RunGitCommand("lfs track",
            proc =>
            {
                if (!proc.WaitForExit(2000))
                {
                    Debug.LogError("VC: LFS type check timed out");
                    return true;
                }
                return false;
            },
            result =>
            {
                var tracked = result.Trim();
                if (tracked.StartsWith("Listing"))
                    return;

                var split = tracked.Split(' ');
                var ext = split[0].Trim();
                LockableExtensions.Add(ext.ToLower().Replace("*.", "."));
            },
            err =>
            {
                Debug.LogError(err);
                return true;
            }
        );
        UpdateLockedFiles();
    }

    public List<string> GenerateRecursiveModifiedList()
    {
        var changes = GetChangedFiles();

        // Getting untracked files
        GitHelper.RunGitCommand("ls-files --others --exclude-standard",
            proc =>
            {
                if (!proc.WaitForExit(500))
                {
                    Debug.Log("[VCS] Timed out waiting for modified file list");
                    return true;
                }
                return false;
            },
            result =>
            {
                if (File.Exists(result))
                {
                    changes.Add(GitHelper.GitToUnityPath(result));
                }
            }
        );

        foreach (var v in changes.ToArray())
        {
            var temp = Path.GetDirectoryName(v);

            // Basically assets
            // Not the best way to do this but fuck it
            while (temp.Length > 5)
            {
                changes.Add(temp.Replace('\\', '/'));
                temp = Path.GetDirectoryName(temp);
            }
        }
        return changes;
    }

    public void UpdateModifiedFiles()
    {
        lock (_actionQueueLock)
        {
            ModifiedPaths.Clear();
            foreach (var v in GenerateRecursiveModifiedList())
            {
                ModifiedPaths.Add(v);
            }
        }
    }

    public void UpdateModifiedFilesAsync()
    {
        try
        {
            var list = GenerateRecursiveModifiedList();
            int modPathHash = 0;
            foreach (var v in ModifiedPaths)
                modPathHash += v.GetHashCode();

            lock (_actionQueueLock)
            {
                _toRunOnMainThread.Enqueue(() =>
                {
                    int nModPathHash = 0;
                    foreach (var v in ModifiedPaths)
                        nModPathHash += v.GetHashCode();

                    if (nModPathHash == modPathHash)
                    {
                        ModifiedPaths.Clear();
                        foreach (var v in list)
                        {
                            ModifiedPaths.Add(v);
                        }
                    }
                });
            }
        }
        catch (System.Exception ex)
        {
            // fail silently, okay here
        }
    }
}