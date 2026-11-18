@echo off
chcp 65001 >nul
cd /d "%~dp0"
if exist "Builds\AgentTown\AgentTown.exe" (
    start "" "%~dp0Builds\AgentTown\AgentTown.exe"
) else (
    echo Open this folder in Unity 6000.6.8f1.
    echo Open Assets/Scenes/AgentTown.unity and press Play.
    echo Use menu AgentTown / 4 to build a Windows player.
    pause
)
