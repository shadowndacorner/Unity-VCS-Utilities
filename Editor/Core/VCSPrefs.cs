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

    public static bool HasKey(string key)
    {
        
        return EditorPrefs.HasKey(ProjectKey + key);
    }

    public static void SetString(string key, string value)
    {
        EditorPrefs.SetString(ProjectKey + key, value);
    }

    public static void SetBool(string key, bool value)
    {
        EditorPrefs.SetBool(ProjectKey + key, value);
    }

    public static void SetInt(string key, int value)
    {
        EditorPrefs.SetInt(ProjectKey + key, value);
    }

    public static void SetFloat(string key, int value)
    {
        EditorPrefs.SetFloat(ProjectKey + key, value);
    }

    public static string GetString(string key)
    {
        return EditorPrefs.GetString(ProjectKey + key);
    }

    public static string GetString(string key, string defaultValue)
    {
        return EditorPrefs.GetString(ProjectKey + key, defaultValue);
    }

    public static bool GetBool(string key)
    {
        return EditorPrefs.GetBool(ProjectKey + key);
    }

    public static bool GetBool(string key, bool defaultValue)
    {
        return EditorPrefs.GetBool(ProjectKey + key, defaultValue);
    }

    public static int GetInt(string key)
    {
        return EditorPrefs.GetInt(ProjectKey + key);
    }

    public static int GetInt(string key, int defaultValue)
    {
        return EditorPrefs.GetInt(ProjectKey + key, defaultValue);
    }

    public static float GetFloat(string key)
    {
        return EditorPrefs.GetFloat(ProjectKey + key);
    }

    public static float GetFloat(string key, float defaultValue)
    {
        return EditorPrefs.GetFloat(ProjectKey + key, defaultValue);
    }

    public static void DeleteKey(string key)
    {
        EditorPrefs.DeleteKey(ProjectKey + key);
    }

    // Unfortunately we can't filter DeleteAll.  Best not to use it.
}
