@echo off

ECHO ------------------------
ECHO MindTouch, Inc. (c) 2006
ECHO http://www.mindtouch.com
ECHO ------------------------

IF "%1"=="" GOTO usage
IF "%2"=="" GOTO usage
IF "%3"=="" GOTO usage

FOR %%f IN (%2) DO curl.exe -u "%1" -F "file_1=@%%f" -F MaxNum=1 -F filedesc_1= "%3?action=attach" -o nul
GOTO :EOF

:USAGE
ECHO.
ECHO USAGE: deki-upload [username:password] [file-pattern] [deki-page]
