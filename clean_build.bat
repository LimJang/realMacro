@echo off
echo ===============================
echo 프로젝트 정리 및 완전 재빌드
echo ===============================

cd /d D:\macro\src\MapleViewCapture

echo.
echo [1단계] 모든 빌드 파일 정리...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo.
echo [2단계] NuGet 캐시 정리...
dotnet nuget locals all --clear

echo.
echo [3단계] 패키지 복원...
dotnet restore --force

echo.
echo [4단계] 완전 재빌드...
dotnet build --configuration Debug --verbosity normal

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ 재빌드 성공!
    echo.
    choice /c YN /m "프로그램을 실행하시겠습니까? (Y/N)"
    if !ERRORLEVEL! EQU 1 (
        start "" "bin\Debug\net6.0-windows\MapleViewCapture.exe"
    )
) else (
    echo.
    echo ❌ 재빌드 실패!
    echo 오류 로그를 확인하세요.
)

echo.
pause
