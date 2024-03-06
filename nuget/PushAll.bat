SET pver=2.1.0
Echo Version: "%pver%"
dir packages\*.nupkg
@echo off
setlocal
:PROMPT
SET AREYOUSURE=N
SET /P AREYOUSURE=Are you sure (Y/[N])?
IF /I "%AREYOUSURE%" NEQ "Y" GOTO END

echo Publishing....
cd packages
:: When we push bin package, the symbols package is pushed automatically by the nuget util
nuget push Arrest.%pver%.nupkg -source https://api.nuget.org/v3/index.json 
pause

:END
endlocal

