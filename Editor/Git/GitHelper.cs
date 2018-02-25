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

public static class GitHelper
{
    public static class VCSPrefsKeys
    {
        public const string UsernamePrefKey = "git-lfs-username";
        public const string SceneAutoLockKey = "git-lfs-autolock-scenes";
        public const string PreventEditsOnLockKey = "git-lfs-prevent-local-edits";

        public static void ResetConfig()
        {
            VCSPrefs.DeleteKey(UsernamePrefKey);
            VCSPrefs.DeleteKey(PreventEditsOnLockKey);
            VCSPrefs.DeleteKey(SceneAutoLockKey);
        }
    }


    public static bool Configured
    {
        get
        {
            return VCSPrefs.HasKey(VCSPrefsKeys.UsernamePrefKey);
        }
    }

    public static bool SceneAutoLock
    {
        get
        {
            if (!VCSPrefs.HasKey(VCSPrefsKeys.SceneAutoLockKey))
                SceneAutoLock = true;

            return VCSPrefs.GetBool(VCSPrefsKeys.SceneAutoLockKey);
        }
        set
        {
            if (VCSPrefs.GetBool(VCSPrefsKeys.SceneAutoLockKey) != value)
                VCSPrefs.SetBool(VCSPrefsKeys.SceneAutoLockKey, value);
        }
    }

    public static bool PreventEditsOnRemoteLock
    {
        get
        {
            if (!VCSPrefs.HasKey(VCSPrefsKeys.PreventEditsOnLockKey))
                PreventEditsOnRemoteLock = true;
            return VCSPrefs.GetBool(VCSPrefsKeys.PreventEditsOnLockKey);
        }
        set
        {
            if (VCSPrefs.GetBool(VCSPrefsKeys.PreventEditsOnLockKey) != value)
                VCSPrefs.SetBool(VCSPrefsKeys.PreventEditsOnLockKey, value);
        }
    }

    public static string Username
    {
        get
        {
            return VCSPrefs.GetString(VCSPrefsKeys.UsernamePrefKey);
        }
        set
        {
            VCSPrefs.SetString(VCSPrefsKeys.UsernamePrefKey, value);
        }
    }

    /// <summary>
    /// Runs a command on the git command line.
    /// </summary>
    /// <param name="cmd">Command line arguments to pass.</param>
    /// <param name="wait">Called when waiting for the process to complete.  Return true to prevent the rest of the functions from executing.  Note that returning true will not kill the process, you must do that manually.</param>
    /// <param name="handleOutputLine">Handles, line by line, the standard output from the process's lifetime.</param>
    /// <param name="handleErrorLine">Handles, line by line, the standard error output from the process's lifetime.  Returning true will prevent handleOutputLine from running, but will not prevent the remaining errors from displaying.  Return false to ignore the errors.</param>
    /// <returns></returns>
    public static bool RunGitCommand(string cmd, System.Func<Process, bool> wait, System.Action<string> handleOutputLine = null, System.Func<string, bool> handleErrorLine = null)
    {
        var startInfo = new ProcessStartInfo();
        startInfo.FileName = "git";
        startInfo.Arguments = cmd;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        var proc = Process.Start(startInfo);
        if (wait(proc))
        {
            return true;
        }

        string result;
        bool error = false;
        while (!proc.StandardError.EndOfStream && (result = proc.StandardError.ReadLine()) != null)
        {
            if (handleErrorLine != null)
            {
                error = error || handleErrorLine(result);
            }
        }

        if (error)
            return true;

        while (!proc.StandardOutput.EndOfStream && (result = proc.StandardOutput.ReadLine()) != null)
        {
            if (handleOutputLine != null)
                handleOutputLine(result);
        }
        return false;
    }

    // These string ops are suboptimal, but they should be realtively infrequent so it
    // doesn't matter too much;

    /// <summary>
    /// Translates a Unity path into the Git
    /// </summary>
    /// <param name="unityPath">The path relative to the Unity project root</param>
    /// <returns></returns>
    public static string UnityToGitPath(string unityPath)
    {
        return Path.Combine(GitVCS.RelativePathToUnityDirectory, unityPath).Replace('\\', '/');
    }

    /// <summary>
    /// Translates a git path into Unity
    /// </summary>
    /// <param name="unityPath">The path relative to the git root</param>
    /// <returns></returns>
    public static string GitToUnityPath(string gitPath)
    {
        var res = gitPath.Replace('\\', '/').Replace(GitVCS.RelativePathToUnityDirectory, "");
        if (res.StartsWith("/"))
            res = res.Substring(1);
        return res;
    }
}
