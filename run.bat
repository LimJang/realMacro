@echo off
echo 메이플랜드 라이브뷰 캡처 프로그램 빌드 및 실행
echo ============================================

cd /d "D:\macro\src\MapleViewCapture"

echo 1. 패키지 복원 중...
dotnet restore

echo 2. 프로젝트 빌드 중...
dotnet build

if %ERRORLEVEL% EQU 0 (
    echo 3. 빌드 성공! 프로그램 실행 중...
    dotnet run
) else (
    echo 빌드 실패! 오류를 확인하세요.
    pause
)
