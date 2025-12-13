# RollSnow(working title)

미니멀 엔드리스 러너 게임입니다. 눈 덮인 슬로프를 내려가며 장애물을 피하고 최대한 멀리, 높은 점수를 달성하는 것이 목표입니다.

- 플랫폼: Android
- 엔진: Unity 6.0
- 장르: Endless Runner / Arcade
- 플레이 시간: 짧게 반복 플레이에 최적화

## 핵심 게임 루프
1. 시작
2. 슬로프 자동 진행
3. 좌우 이동으로 장애물 회피
4. 거리 기반 점수 증가
5. 충돌 시 게임 오버
6. 최고점 갱신 및 재시작

## 조작
- 탭: 좌측 탭은 왼쪽, 우측 탭은 오른쪽 이동

현재 프로젝트에서 실제 적용한 방식에 맞춰 위 문장 중 하나만 남기면 됩니다.

## 주요 기능
- 엔드리스 진행 맵(세그먼트 재활용)
- 장애물 스폰 및 충돌 처리
- 점수(거리) 및 최고점 로컬 저장
- 게임 오버, 재시작 UI
- 난이도 증가(시간에 따른 속도 증가)

## 스크린샷 / 데모
- Gameplay GIF: docs/media/gameplay.gif
- Screenshots: docs/media/

## 개발 환경
- Unity: 2022.3.x LTS 권장
- Build Target: Android
- Scripting Backend: IL2CPP 권장
- Target API: 35 이상 권장

## 프로젝트 열기
1. Unity Hub 실행
2. Add project from disk 선택
3. 이 레포 폴더 선택
4. Open

## 메인 씬
- Assets/Scenes/Main.unity

프로젝트에 맞는 씬 경로로 수정하세요.

## 폴더 구조
- Assets/
  - Scenes/
  - Scripts/
    - Gameplay/
    - Input/
    - UI/
    - Systems/
  - Prefabs/
  - Art/
  - Audio/
- docs/
  - media/
- ProjectSettings/

## 실행 방법
- Unity 에디터에서 Main 씬을 열고 Play 버튼 실행

## Android 빌드
1. File > Build Settings
2. Android 선택 후 Switch Platform
3. Player Settings에서 Package Name 설정
4. Build App Bundle (Google Play) 체크
5. Build 버튼으로 AAB 생성

릴리즈 키스토어와 서명은 팀 내 보안 정책에 따라 관리하세요.

## Git 협업 규칙
- main: 항상 실행 가능한 상태
- develop: 통합 브랜치
- feature/xxx: 기능 단위 개발 브랜치

권장 흐름
- feature 브랜치 생성
- 작업 후 PR 생성
- 리뷰 후 develop 병합
- 릴리즈 시 main 병합 및 태그

## 코드 스타일
- public API는 간결하게
- Update는 최소화, 필요 시 FixedUpdate 또는 이벤트 기반 사용
- 오브젝트 풀링을 기본으로 사용
- 로그는 English 권장

## 이슈 등록 가이드
버그 리포트에 포함할 항목
- 재현 단계
- 기대 결과
- 실제 결과
- 기기 정보, OS 버전
- 스크린샷 또는 영상

개선 제안에 포함할 항목
- 문제 설명
- 제안 내용
- 기대 효과
- 우선순위

## 로드맵
- v0.1 MVP
  - 이동, 장애물, 점수, 게임 오버
- v0.2 폴리시 및 UX
  - 튜토리얼, 설정, 난이도 튜닝
- v1.0 출시
  - 스토어 자산, 최종 빌드, 안정화

## 크레딧
- 기획: hdbanana4
- 개발: jaw-choi
- 아트: hdbanana4
- 사운드: jaw-choi

## 라이선스
- 추후 결정
