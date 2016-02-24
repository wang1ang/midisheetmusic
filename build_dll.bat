REM Script to build MidiSheetMusic.exe from the command prompt.
REM Modify the PATH to the directory containing the csc (C# compiler)

PATH=%PATH%;C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319;

MSbuild.exe SheetMusicDLL.csproj

