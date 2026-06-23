# 🧀 드래프트 카드 Udon 셋업 가이드

`Udon/` 폴더 스크립트로 드래프트 전 과정을 네트워크 동기화로 구현합니다.

| 스크립트 | 붙이는 곳 | 역할 |
|----------|-----------|------|
| `DraftBoard.cs` | 빈 GameObject 1개 | 카드·차례·뺏기·동기화 + 흐름 제어 |
| `DraftCard.cs` | 카드마다 | 누르기 + 미리보기/도킹/복귀 + 4스탯 데이터 |
| `TeamRoster.cs` | 팀 패널 5개 | 뽑힌 카드가 도킹될 슬롯 |
| `ScreenController.cs` | 메인 스크린 | 선택 카드 크게 + "잠깐만!" 뺏기 오버레이 |
| `DraftActionButton.cs` | 확정/취소/리셋/뺏어오기 버튼 | 버튼 → 보드 동작 |
| `LeaderConsole.cs` | 팀장 좌석 작은 스크린 | ◀▶ 후보 조회(인물+이름+4스탯+X) |
| `LeaderConsoleButton.cs` | 콘솔 ◀▶ 버튼 | 콘솔 넘기기 |

> ⚠️ **VRChat SDK3(Worlds) + UdonSharp 프로젝트 전용.** 레이아웃 확인용 빈 프로젝트엔 넣지 마세요.

---

## 전체 흐름
1. 그리드 카드 **누름** → 카드가 떠오르고 **메인 스크린에 인물 크게**(확정 대기).
2. (선택) 다른 팀장이 **뺏어오기(빨간) 버튼** → 메인 스크린에 **"잠깐만! 뺏어오기!"** + 누른 팀장 **이름 누적**.
   타겟 팀 = **마지막으로 누른 팀**(다시 누르면 빠짐). 누가 이길지는 외부 규칙으로 정함.
3. **확정** → 카드가 **현재 타겟 팀 패널로 날아가 도킹**(아무도 안 뺏으면 원래 고른 팀), 다음 차례.
   **취소** → 카드 원위치.
- **팀장 개인 콘솔**: 좌석마다 ◀▶로 후보를 넘겨보며 4스탯 조회. 확정된 후보는 **X(선택완료)+팀** 표시. 메인 무관.

---

## 🖥️ 화면(스크린) 띄우는 원리
화면 면(메쉬)의 **머티리얼 텍스처를 Udon이 교체**하는 방식입니다(오브젝트를 매번 붙이는 게 아님).
- **3D 프레임**(이번에 제작 예정): 모델의 파란 화면 면 Renderer를 `displayRenderer`에 연결. 끝.
- 발광시키려면 머티리얼 Emission ON + `textureProperty`를 `_EmissionMap`으로(기본 `_MainTex`).
- 영상이 필요하면 같은 면에 Video Player + RenderTexture를 물리면 됨(카드 표시엔 텍스처 교체로 충분).

---

## 셋업 단계

### 1. 카드 (DraftCard)
- 카드 프리팹: 얇은 박스/쿼드 + **콜라이더** + 앞면 인물.
- `DraftCard` 추가 → `playerName`, `portrait`(인물 Texture), **4스탯**(`gameSkill`/`gameSense`/`teamwork`/`luck` = 게임실력/게임센스/협동력/운), `nameLabel`(선택).
- 인원수만큼 그리드 배치.

### 2. 메인 스크린 (ScreenController)
- 3D 프레임의 파란 화면 면 Renderer를 `displayRenderer`에 연결, `idleTexture`(대기 로고) 선택.
- (선택) `pendingOverlay`(확정 대기 테두리), **`stealOverlay`(빨간 "잠깐만" 화면)**, `stealNamesLabel`(이름 목록 TextMesh) 연결.

### 3. 팀 패널 5개 (TeamRoster)
- 각 패널에 `TeamRoster` → `teamName`(열무~오늘), `slots`(도킹 자리 빈 오브젝트 위→아래), `turnIndicator`(선택).

### 4. 버튼들 (DraftActionButton)
- 확정 버튼 `action=0` / 취소 `action=1` / (선택) 리셋 `action=2`, 각각 `board` 연결.
- **뺏어오기 버튼**(팀장 좌석 빨간 버튼): `action=3`, **`teamIndex`에 그 좌석 팀 번호**(열무=0 … 오늘=4), `board` 연결.
- 확정·취소 버튼을 한 부모로 묶어 `DraftBoard.confirmCancelUI`에 연결 → 미리보기 중에만 자동 표시.

### 5. 팀장 개인 콘솔 (LeaderConsole)
- 좌석마다 작은 스크린(쿼드) + ◀▶ 버튼(콜라이더).
- 작은 스크린 오브젝트에 `LeaderConsole` → `board`, `displayRenderer`(스크린 면), `nameLabel`/`statsLabel`/`takenLabel`(TextMesh), `xMark`(확정 시 켜질 X 오브젝트) 연결.
- ◀▶ 버튼에 `LeaderConsoleButton` → `console`(그 콘솔), `dir`(◀ = -1, ▶ = +1).

### 6. 보드 (DraftBoard) 마무리
- 빈 오브젝트에 `DraftBoard` → `cards`(카드 전부) / `teams`(팀 패널 5개, 열무→오늘) / `screen` / `confirmCancelUI` / `enforceTurnOrder`.

---

## 작동 방식 (요약)
- **상태는 보드 하나만 동기화**: 카드별 소유팀 / 미리보기 카드 / 원픽·타겟 팀 / 뺏기 호출(팀별) / 차례.
- 개인 콘솔은 좌석별로 따로 동기화(보는 후보 인덱스).
- 변경한 사람이 소유권 가져와 갱신 → `RequestSerialization` → 모두 `OnDeserialization`에서 동일 반영.

## 남은 것 / 다음
- (선택) **자동 배치 빌더**: 카드 그리드·팀 슬롯·버튼·콘솔·임시 디스플레이 면을 씬에 자동 생성해 연결 노가다 축소.
- **권한 제한**: 지금은 누구나 픽/확정/뺏기 가능 — 팀장만 하게 하려면 각 입력에 플레이어 팀 확인 추가.

## 검증 메모
- **UdonSharp는 VRChat SDK가 있어야 컴파일**되어 이 환경에서 자동 검증은 못 했습니다. VRChat API 규칙
  (Manual 동기화·소유권 이전·`RequestSerialization`·`OnDeserialization`)에 맞춰 작성했습니다.
  실제 프로젝트에서 컴파일 시 에러가 뜨면 메시지 그대로 주세요 — 바로 잡아드립니다.
