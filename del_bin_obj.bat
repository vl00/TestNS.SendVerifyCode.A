@echo off
set nowpath=%cd%
cd \
cd %nowpath%

REM delete specify folder(obj,bin)
for /r "%nowpath%" %%i in (obj,bin,.vs) do (
    if exist "%%i" (
        set "_b="
        if exist "%%i\..\*.csproj" set _b=1
        if exist "%%i\..\*.xproj" set _b=1
        if exist "%%i\..\*.vbproj" set _b=1
        if exist "%%i\..\*.vcproj" set _b=1
        if exist "%%i\..\*.fsproj" set _b=1
        if defined _b (
            echo delete %%i
            rd /s /q "%%i"
        )
    )
)

::pause