# 메이플랜드 라이브뷰 캡처 프로그램

## 빌드 및 실행 방법

### 1. 프로젝트 빌드
```bash
cd D:\macro\src\MapleViewCapture
dotnet restore
dotnet build
```

### 2. 실행
```bash
dotnet run
```

## 현재 기능 (Phase 1)
- ✅ 게임 창 찾기 (FindWindow API)
- ✅ 게임 창 크기 자동 조정 (800x600)
- ✅ BitBlt API를 이용한 실시간 캡처
- ✅ 100ms 간격 캡처
- ✅ 실시간 미리보기

## 사용 방법
1. 메이플랜드 게임 실행
2. 프로그램 실행
3. "게임 창 찾기" 버튼 클릭
4. "캡처 시작" 버튼 클릭

## 다음 단계 (Phase 2)
- ROI 영역 설정 기능
- 템플릿 생성 도구
- OpenCV 템플릿 매칭

## 트러블슈팅
- 게임 창을 찾을 수 없는 경우: 게임 창 제목 확인 필요
- 캡처가 검은색으로 나오는 경우: 관리자 권한으로 실행
