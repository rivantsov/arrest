SET pver=1.4.0
Echo Version: "%pver%"
dir ..\Arrest\bin\Release\*.%pver%.nupkg
@echo off
setlocal
:PROMPT
SET AREYOUSURE=N
SET /P AREYOUSURE=Are you sure (Y/[N])?
IF /I "%AREYOUSURE%" NEQ "Y" GOTO END

echo Publishing....
:: When we push bin package, the symbols package is pushed automatically by the nuget util
nuget push ..\Arrest\bin\Release\Arrest.%pver%.nupkg -source https://api.nuget.org/v3/index.json 
pause

:END
endlocal

