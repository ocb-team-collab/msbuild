this folder is self-contained MsBuild project files used for testing things

the batch files are usually used in these order:

dominobuild: runs the test case
piplist: gets all the pips in the build 
dbgonlaunchon: turns on debugging at boot for msbuild.exe and tasklauncher.exe
dbgonlaunchoff: turns off debugging at boot (usually do this before a dominobuild to prevent a bunch of popups)
piprun: runs a pip (usually found from piplist above) and usually done with dbgonlaunchon so you can attach
