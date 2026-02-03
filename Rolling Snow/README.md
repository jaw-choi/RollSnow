# Rolling Snow - Backend/Firebase Plan

이 문서는 `BackendManager.cs` 기준의 현재 백엔드 사용 현황과, Firebase로 전환/확장하려는 계획(특히 매일 랭킹 보상 지급)을 정리합니다.

## 현재 BackendManager.cs 역할 요약
- 싱글톤 + 씬 간 유지 옵션 (`persistBetweenScenes`)
- 자동 로그인 플로우 (`autoLoginOnStart`)
- `GameManager` 이벤트 연결/해제 (`HighScoreUpdated`)
- 닉네임 최초 설정 유도 및 패널 자동 오픈
- 랭킹 패널 자동 오픈 요청/보류 처리

## 현재 사용 중인 BackEnd SDK 기능
- 초기화: `Backend.Initialize()`
- 커스텀 계정
  - 회원가입: `BackendLogin.Instance.CustomSignUp(id, pw)`
  - 로그인: `BackendLogin.Instance.CustomLogin(id, pw)`
- 닉네임
  - 닉네임 조회: `BackendLogin.Instance.TryGetNickname(out nickname)`
  - 중복 확인: `BackendLogin.Instance.CheckNickname(nickname)`
  - 변경: `BackendLogin.Instance.UpdateNickname(nickname)`
- 게임 데이터
  - 데이터 행 보장: `BackendGameData.Instance.EnsureRowInDate()`
  - 데이터 업데이트: `BackendGameData.Instance.GameDataUpdate(null, nickname)`
- 랭킹
  - 점수 삽입: `BackendRank.Instance.RankInsert(score)`

## 로컬 저장/상태 관리
- PlayerPrefs
  - 커스텀 ID/PW 저장: `Backend.CustomId`, `Backend.CustomPw`
  - 닉네임 저장: `Backend.Nickname`
  - 하이스코어 fallback: `HighScore`
- 상태 플래그
  - `IsInitialized`, `IsLoggedIn`, `UserId`, `Nickname`
  - 보류 플래그: `pendingAutoOpenRanking`, `pendingRequireNickname`

## Firebase 전환/확장 목표
- BackEnd SDK 의존을 줄이고 Firebase 기반으로 대체 또는 병행
- 인증, 데이터, 랭킹, 보상 지급 로직을 Firebase로 일원화
- 치팅/조작을 막기 위해 핵심 보상 처리는 서버에서 수행

## 매일 랭킹 보상 지급 플로우(서버 중심)
1) 매일 특정 시각에 스케줄 함수 실행 (예: 21:00 KST)
2) 당일(또는 전일) 리더보드에서 Top 3 조회
3) `reward_runs/{yyyyMMdd}` 형태의 레코드로 중복 지급 방지
4) `users/{uid}/rewards/{yyyyMMdd}` 또는 유저 인벤토리에 보상 적립
5) 클라이언트는 로그인 시 미수령 보상 조회/표시/수령

### 권장 구성(Firebase 기준)
- Cloud Functions + Scheduler: 매일 정해진 시간에 자동 실행
- Firestore 또는 Realtime DB: 랭킹 저장 및 조회
- 보상 지급 기록 컬렉션: 중복 지급 방지 및 감사 로그

## 결정해야 할 사항
- 리더보드 데이터 저장소: Firestore vs Realtime DB
- 기준 시간/타임존: 예) 매일 21:00 KST
- 보상 종류/수량: 코인, 아이템, 스킨 등
- 동점 처리 규칙: 동점자 처리 방식
- 지급 기준 날짜: 당일 결과 vs 전일 결과
- 보상 수령 방식: 자동 지급 vs 클라이언트 수령 버튼

## 다음 단계 제안
- Firebase 데이터 모델 및 스키마 설계
- Cloud Functions 스케줄 함수 설계/구현
- Unity 클라이언트: 보상 조회/표시/수령 UX 설계
- 기존 `BackendManager.cs` 기능 매핑 및 단계적 전환 계획 수립
