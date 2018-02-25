# Unity Version Control Utilities
This is an editor utility which adds much nicer support for VCS in the Unity Editor.  I made it for usage in my own projects, but figured other people might find it useful.

Currently only git support is implemented, however there is an abstract interface to add support for whatever your preferred VCS is.  As for git, all functionality is run with the command line so it should theoretically support your preferred git host.  However, it has only been tested with GitHub.

Note that this project is not designed to be an all-in-one VCS client.  It is meant to be used in addition to an external VCS client.  This is explicitly meant to make the workflow of integrating with an external client easier for users who are less versed in how VCS works.  For example, I personally use Github Desktop and the git command line with this.

# Features
This utility is heavily inspired heavily by Unity Collaborate's integration with the Unity editor.  All of the following features are seamlessly integrated into the Unity editor, primarily in the Project panel.  It currently supports:
* File Locking
	* Shows the user that modified the file
	* Option to prevent saving files modified by other users
	* Option to auto-lock scenes on edit
* Display of file modifications
	* Displays recursively in the project panel
* Ability to discard changes from within the editor
	* Supports multi-select, including recursive discards for directories
* Rudimentary support for Unity's YamlSmartMerge.  This needs improvement

# Pre-requisites
This has only been tested on Windows, but as it uses the git command line, there is little reason for it to not work on OSX.  If running Windows, you need at least Git for Windows 2.16 for LFS support on GitHub.

# Installation
Simply add this directory somewhere in your project (it can even be added as a git submodule).

# Usage
All functionality is contained in two places - Assets/Version Control (also the right click menu for assets) and the Version Control section of the toolbar.

## Git
The first time this is launched, it will ask you to set your git username in the configuration window.  Assuming your repository is already using LFS, this should be the only required configuration step.

After this initial step, all interactions can be done through either the right-click menu in the Project window or 

## Other VCS's
Git is the only VCS currently implemented, but it should be fairly easy to implement other VCS's such as SVN or Perforce.  

# Known Issues
## Git
The Git workflow currently assumes that the Git root directory is also the Unity project's root directory (ie, .git/ is next to Assets/).  This is one of the first things I aim to address.