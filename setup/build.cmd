@echo off
::Creates archives and and Inno Setup installer. Assumes "..\src\build.cmd Release" and "..\bundled\download-solver.ps1" have already been executed.
::Use command-line argument "+update" to additionally create updater archive.
if "%1"=="+updater" set BUILD_UPDATER=TRUE
if "%2"=="+updater" set BUILD_UPDATER=TRUE
if "%3"=="+updater" set BUILD_UPDATER=TRUE
if "%4"=="+updater" set BUILD_UPDATER=TRUE

rem Project settings
set TargetDir=%~dp0..\build\Setup


rem Prepare clean output directory
if not exist "%TargetDir%" mkdir "%TargetDir%"
del /q "%TargetDir%\*"

rem Read version numbers
set /p version= < "%~dp0version"
set /p version_tools= < "%~dp0version-tools"
set /p version_updater= < "%~dp0version-updater"
copy "%~dp0version" "%TargetDir%\version" > NUL
copy "%~dp0version-tools" "%TargetDir%\version-tools" > NUL
if "%BUILD_UPDATER%"=="TRUE" copy "%~dp0version_updater" "%TargetDir%\version_updater" > NUL

rem Use bundled utility EXEs
path %~dp0utils;%path%

echo Building ZIP archive...
cd /d "%~dp0..\bundled"
zip -q -9 -r "%TargetDir%\zero-install.zip" GnuPG Solver
if errorlevel 1 pause
cd /d "%~dp0..\build\Frontend\Release"
zip -q -9 -r "%TargetDir%\zero-install.zip" . --exclude *.log *.mdb *.vshost.exe Test.* nunit.* Mono.* *.pdb *.xml
if errorlevel 1 pause
zip -q -9 -j "%TargetDir%\zero-install.zip" "%~dp0..\license.txt"
if errorlevel 1 pause
zip -q -9 -j "%TargetDir%\zero-install.zip" "%~dp0..\3rd party code.txt"
if errorlevel 1 pause
cd /d "%~dp0"

echo Building TAR.BZ2 archive...
bsdtar -cjf "%TargetDir%\zero-install-%version%.tar.bz2" --exclude=*.log --exclude=*.mdb --exclude=*.vshost.exe --exclude=Test.* --exclude=nunit.* --exclude=Mono.* --exclude=*.pdb --exclude=*.xml -C "%~dp0.." "license.txt" -C "%~dp0.." "3rd party code.txt" -C "%~dp0..\bundled" GnuPG Solver -C "%~dp0..\build\Frontend\Release" .
if errorlevel 1 pause

echo Building Tools archive...
bsdtar -cjf "%TargetDir%\zero-install-tools-%version_tools%.tar.bz2" --exclude=*.log --exclude=*.mdb --exclude=*.vshost.exe --exclude=Test.* --exclude=nunit.* --exclude=Mono.* --exclude=*.pdb --exclude=*.xml -C "%~dp0.." "license.txt" -C "%~dp0.." "3rd party code.txt" -C "%~dp0..\build\Tools\Release" .
if errorlevel 1 pause

echo Building Tools developer archive...
cd /d "%~dp0..\bundled"
zip -q -9 -r "%TargetDir%\zero-install-tools-dev.zip" GnuPG
cd /d "%~dp0..\build\Tools\Release"
zip -q -9 -r "%TargetDir%\zero-install-tools-dev.zip" . --exclude *.log *.mdb *.vshost.exe Test.* nunit.* Mono.*
if errorlevel 1 pause
zip -q -9 -j "%TargetDir%\zero-install-tools-dev.zip" "%~dp0..\license.txt"
if errorlevel 1 pause
zip -q -9 -j "%TargetDir%\zero-install-tools-dev.zip" "%~dp0..\3rd party code.txt"
if errorlevel 1 pause
cd /d "%~dp0"

echo Building Backend developer archive...
cd /d "%~dp0..\bundled"
zip -q -9 -r "%TargetDir%\zero-install-backend-dev.zip" GnuPG Solver
cd /d "%~dp0..\build\Backend\Release"
zip -q -9 -r "%TargetDir%\zero-install-backend-dev.zip" . --exclude *.log *.mdb *.vshost.exe Test.* nunit.* Mono.*
if errorlevel 1 pause
zip -q -9 -j "%TargetDir%\zero-install-backend-dev.zip" "%~dp0..\license.txt"
if errorlevel 1 pause
zip -q -9 -j "%TargetDir%\zero-install-backend-dev.zip" "%~dp0..\3rd party code.txt"
if errorlevel 1 pause
cd /d "%~dp0"

if "%BUILD_UPDATER%"=="TRUE" (
  echo Building Updater archive...
  bsdtar -cjf "%TargetDir%\zero-install-updater-%version_updater%.tar.bz2" --exclude=*.log --exclude=*.pdb --exclude=*.mdb --exclude=*.vshost.exe --exclude=Test.* --exclude=nunit.* --exclude=Mono.* --exclude=*.xml --exclude=SevenZip.* --exclude=C5.* -C "%~dp0..\build\Updater\Release" .
  if errorlevel 1 pause
)

rem Handle WOW
if %PROCESSOR_ARCHITECTURE%==x86 set ProgramFiles_temp=%ProgramFiles%
if not %PROCESSOR_ARCHITECTURE%==x86 set ProgramFiles_temp=%ProgramFiles(x86)%

rem Check for Inno Setup 5 installation (32-bit)
if not exist "%ProgramFiles_temp%\Inno Setup 5" (
  echo ERROR: No Inno Setup 5 installation found. >&2
  pause
  goto end
)

echo Building installer...
cd /d "%~dp0"
"%ProgramFiles_temp%\Inno Setup 5\iscc.exe" /q "/dVersion=%version%" setup.iss
if errorlevel 1 pause

if "%1"=="+run" "%TargetDir%\zero-install.exe" /silent
if "%2"=="+run" "%TargetDir%\zero-install.exe" /silent
if "%3"=="+run" "%TargetDir%\zero-install.exe" /silent
if "%4"=="+run" "%TargetDir%\zero-install.exe" /silent

:end
