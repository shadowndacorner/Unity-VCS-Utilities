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

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public abstract class AbstractVCSHelper
{
    public static void SetMetadata(AbstractVCSHelper helper, VCSImplementationAttribute metadata)
    {
        helper.Metadata = metadata;
    }

    public VCSImplementationAttribute Metadata
    {
        get;
        internal set;
    }

    protected Object ActiveTarget
    {
        get
        {
            return Selection.activeObject;
        }
    }

    protected string ActiveTargetPath
    {
        get
        {
            return AssetDatabase.GetAssetPath(Selection.activeObject);
        }
    }

    protected string[] TargetPaths
    {
        get
        {
            var guids = Selection.assetGUIDs;
            string[] paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; ++i)
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);

            return paths;
        }
    }

    public enum ContextMenuButton
    {
        Lock,
        Unlock,
        DiscardChanges
    }

    public virtual bool IsActive
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public virtual List<string> GetChangedFiles()
    {
        throw new System.NotImplementedException();
    }

    public virtual List<string> GetTrackedFiles()
    {
        throw new System.NotImplementedException();
    }

    public virtual void DiscardChanges()
    {
        throw new System.NotImplementedException();
    }

    public virtual bool IsFileLockable(string path)
    {
        throw new System.NotImplementedException();
    }

    public virtual bool IsFileLocked(string path)
    {
        throw new System.NotImplementedException();
    }

    public virtual bool IsFileLockedByLocalUser(string path)
    {
        throw new System.NotImplementedException();
    }

	public virtual bool LockFile()
    {
        throw new System.NotImplementedException();
    }

    public virtual bool UnlockFile()
    {
        throw new System.NotImplementedException();
    }

    public virtual void Initialize()
    {
        throw new System.NotImplementedException();
    }

    public virtual void Update()
    {
        // this can be empty
    }

    public virtual void RefreshLockedFiles()
    {
        throw new System.NotImplementedException();
    }

    public virtual void ConfigMenuOnGui()
    {
        throw new System.NotImplementedException();
    }

    public virtual bool ContextMenuButtonEnabled(ContextMenuButton button)
    {
        throw new System.NotImplementedException();
    }

    public virtual void HandleModifiedAsset()
    {

    }
}
