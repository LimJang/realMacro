@echo off
echo ===============================
echo 메이플랜드 캡처 프로그램 실행
echo ===============================

cd /d D:\macro\src\MapleViewCapture

echo.
echo 실행파일 확인 중...

if exist "bin\Debug\net6.0-windows\MapleViewCapture.exe" (
    echo ✅ 실행파일 발견: MapleViewCapture.exe
    echo.
    echo 🚀 프로그램 실행 중...
    echo.
    echo 📋 사용 방법:
    echo 1. 메이플랜드 게임을 먼저 실행하세요
    echo 2. 프로그램에서 "게임 창 찾기" 클릭
    echo 3. "캡처 시작" 클릭
    echo 4. "템플릿 매칭 시작" 클릭하면 실시간 인식 시작
    echo.
    start "" "bin\Debug\net6.0-windows\MapleViewCapture.exe"
    echo 프로그램이 시작되었습니다!
) else (
    echo ❌ 실행파일을 찾을 수 없습니다.
    echo.
    echo 다음 중 하나를 시도하세요:
    echo 1. build_and_run.bat 실행 (빌드 후 실행)
    echo 2. Visual Studio에서 프로젝트 빌드
    echo.
)

echo.
pause
