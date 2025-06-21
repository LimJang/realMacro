@echo off
echo ===============================
echo 메이플랜드 캡처 프로그램 디버그
echo ===============================

cd /d D:\macro\src\MapleViewCapture

echo.
echo [디버그 모드] 콘솔과 함께 실행...
echo 오류 메시지와 로그를 확인할 수 있습니다.
echo.

if exist "bin\Debug\net6.0-windows\MapleViewCapture.exe" (
    echo ✅ 실행파일 발견
    echo.
    echo 🔍 디버그 정보와 함께 실행 중...
    "bin\Debug\net6.0-windows\MapleViewCapture.exe"
    echo.
    echo 프로그램이 종료되었습니다.
) else (
    echo ❌ 실행파일을 찾을 수 없습니다.
    echo build_and_run.bat을 먼저 실행하세요.
)

echo.
pause
