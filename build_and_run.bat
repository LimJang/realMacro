@echo off
echo ======================================
echo 메이플랜드 캡처 프로그램 빌드 및 실행
echo ======================================

cd /d D:\macro\src\MapleViewCapture

echo.
echo [1단계] 기존 빌드 파일 정리...
if exist bin\Debug rmdir /s /q bin\Debug
if exist obj\Debug rmdir /s /q obj\Debug

echo.
echo [2단계] NuGet 패키지 복원...
dotnet restore --force

echo.
echo [3단계] 프로젝트 빌드...
dotnet build --configuration Debug --no-restore

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ 빌드 실패! 기존 실행파일로 실행을 시도합니다...
    echo.
    echo [대안] 기존 빌드된 실행파일 실행...
    if exist "bin\Debug\net6.0-windows\MapleViewCapture.exe" (
        echo ✅ 기존 실행파일 발견, 실행 중...
        start "" "bin\Debug\net6.0-windows\MapleViewCapture.exe"
    ) else (
        echo ❌ 실행파일을 찾을 수 없습니다.
        pause
        exit /b 1
    )
) else (
    echo.
    echo ✅ 빌드 성공! 프로그램을 실행합니다...
    echo.
    echo [4단계] 프로그램 실행...
    start "" "bin\Debug\net6.0-windows\MapleViewCapture.exe"
)

echo.
echo 프로그램이 시작되었습니다.
echo 창이 열리지 않으면 작업표시줄을 확인하세요.
echo.
pause
