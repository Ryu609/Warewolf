cd %~dp0..
powershell -ExecutionPolicy Bypass -File %~dp0..\Compile.ps1 --AcceptanceTesting -SolutionWideBuildOutputs %1 %2 %3 %4 %5