@echo off
REM train.bat - teach a robot at LYCEA, the fleet's school, with ML-Agents.
REM   train.bat NoraAvoid      (or NoraLine, WhipWalk)
REM Then press Play in Unity on the matching scene (Assets/Scenes/NORA_Avoid, NORA_Line, WHIP_Walk).
REM ML-Agents needs Python 3.10: uv fetches it into .\venv the first time.
setlocal
cd /d "%~dp0"
set "B=%~1"
if "%B%"=="" set "B=NoraAvoid"
if not exist "venv\Scripts\python.exe" (
    uv venv venv --python 3.10 || goto :fail
    uv pip install --python venv\Scripts\python.exe mlagents==1.1.0 || goto :fail
)
echo Press Play in Unity when it says "Listening on port 5004".
set "MORE="
if exist "results\%B%" set "MORE=--resume"
venv\Scripts\mlagents-learn.exe "config\%B%.yaml" --run-id=%B% %MORE%
echo The trained model is in results\%B%\%B%.onnx - drop it on the agent's Behavior Parameters ^> Model.
pause
exit /b

:fail
echo Setup failed - is uv installed? (https://docs.astral.sh/uv/)
pause
exit /b 1
