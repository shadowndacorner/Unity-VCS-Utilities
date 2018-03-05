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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Wrapper class to prepend EditorPrefs with per-project key
public static class VCSPrefs
{
    static string ProjectKey
    {
        get
        {
            // This is absurdly long.  Oh well.
            return "VCS_Settings_" + System.Environment.CurrentDirectory;
        }
    }

    public static bool IsMainThread
    {
        get
        {
            if (_thread == null)
            {
                // This is a dirty hack
                bool isOnMainThread = false;
                try
                {
                    EditorPrefs.HasKey("test"); // this will fail outside of the main thread
                    isOnMainThread = true;
                }
                catch (System.Exception ex)
                {

                }
                return isOnMainThread;
            }
            return _thread == System.Threading.Thread.CurrentThread;
        }
    }
    static System.Threading.Thread _thread;
    static Dictionary<string, object> _values = new Dictionary<string, object>();
    static object _fileLock = new object();
    public static bool HasInitializedKeys
    {
        get
        {
            return IsMainThread || _values.Count > 0;
        }
    }

    public static bool HasKey(string key)
    {
        lock (_fileLock)
        {
            if (!IsMainThread)
            {
                return _values.ContainsKey(key);
            }

            return EditorPrefs.HasKey(ProjectKey + key);
        }
    }

    public static void SetString(string key, string value)
    {
        lock (_fileLock)
        {
            EditorPrefs.SetString(ProjectKey + key, value);
            _values[key] = value;
            _thread = System.Threading.Thread.CurrentThread;
        }
    }

    public static void SetBool(string key, bool value)
    {
        lock (_fileLock)
        {
            EditorPrefs.SetBool(ProjectKey + key, value);
            _values[key] = value;
            _thread = System.Threading.Thread.CurrentThread;
        }
    }

    public static void SetInt(string key, int value)
    {
        lock (_fileLock)
        {
            EditorPrefs.SetInt(ProjectKey + key, value);
            _values[key] = value;
            _thread = System.Threading.Thread.CurrentThread;
        }
    }

    public static void SetFloat(string key, int value)
    {
        lock (_fileLock)
        {
            EditorPrefs.SetFloat(ProjectKey + key, value);
            _values[key] = value;
            _thread = System.Threading.Thread.CurrentThread;
        }
    }

    public static string GetString(string key)
    {
        lock (_fileLock)
        {
            if (!IsMainThread)
                return _values.ContainsKey(key) ? (string)_values[key] : null;

            return EditorPrefs.GetString(ProjectKey + key);
        }
    }

    public static string GetString(string key, string defaultValue)
    {
        lock (_fileLock)
        {
            if (!IsMainThread)
                return _values.ContainsKey(key) ? (string)_values[key] : defaultValue;

            return EditorPrefs.GetString(ProjectKey + key, defaultValue);
        }
    }

    public static bool GetBool(string key)
    {
        lock (_fileLock)
        {
            if (!IsMainThread)
                return _values.ContainsKey(key) ? (bool)_values[key] : false;

            return EditorPrefs.GetBool(ProjectKey + key);
        }
    }

    public static bool GetBool(string key, bool defaultValue)
    {
        lock (_fileLock)
        {
            if (!IsMainThread)
                return _values.ContainsKey(key) ? (bool)_values[key] : defaultValue;

            return EditorPrefs.GetBool(ProjectKey + key, defaultValue);
        }
    }

    public static int GetInt(string key)
    {
        lock (_fileLock)
        {
            if (!IsMainThread)
                return _values.ContainsKey(key) ? (int)_values[key] : 0;

            return EditorPrefs.GetInt(ProjectKey + key);
        }
    }

    public static int GetInt(string key, int defaultValue)
    {
        lock (_fileLock)
        {
            if (!IsMainThread)
                return _values.ContainsKey(key) ? (int)_values[key] : defaultValue;

            return EditorPrefs.GetInt(ProjectKey + key, defaultValue);
        }
    }

    public static float GetFloat(string key)
    {
        lock (_fileLock)
        {
            if (!IsMainThread)
                return _values.ContainsKey(key) ? (float)_values[key] : 0;

            return EditorPrefs.GetFloat(ProjectKey + key);
        }
    }

    public static float GetFloat(string key, float defaultValue)
    {
        lock (_fileLock)
        {
            if (!IsMainThread)
                return _values.ContainsKey(key) ? (float)_values[key] : defaultValue;

            return EditorPrefs.GetFloat(ProjectKey + key, defaultValue);
        }
    }

    public static void DeleteKey(string key)
    {
        lock (_fileLock)
        {
            EditorPrefs.DeleteKey(ProjectKey + key);
            _thread = System.Threading.Thread.CurrentThread;
        }
    }

    // Unfortunately we can't filter DeleteAll.  Best not to use it.
}
