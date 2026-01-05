# 🥁 SamulVerse: 흔들리는 거북선

<div align="center">

![Unity](https://img.shields.io/badge/Unity-2021.3_LTS-black?logo=unity)
![Meta XR](https://img.shields.io/badge/Meta_XR-SDK-blue)
![Quest](https://img.shields.io/badge/Quest-2%20%7C%203-lightgrey)
![Status](https://img.shields.io/badge/Status-In_Development-yellow)

**VR 리듬 게임 - 한국 전통 사물놀이 × 조선 거북선**

</div>

---

## 📖 프로젝트 소개

**SamulVerse: 흔들리는 거북선**은 한국 전통 타악기 사물놀이와 조선시대 거북선을 결합한 VR 리듬 게임입니다. 
플레이어는 거북선 위에서 4개의 한국 전통 북을 연주하며, 리듬에 맞춰 NPC가 노를 젓는 속도가 변화합니다.

### 🎯 개발 목표

- 한국 전통 문화 콘텐츠의 글로벌화
- 고립된 청소년/노년층 대상 치료적 활용
- K-Culture 홍보 및 교육

---

## 🎮 주요 기능

### ✨ 게임플레이

- **4-Drum 레이아웃**: 한국 전통 타악기 배치
- **VR 타격 시스템**: Meta Quest 2/3 컨트롤러 지원
- **실시간 판정**: Perfect / Good / Miss
- **콤보 시스템**: 콤보에 따라 NPC 노 젓기 속도 증가
- **Obstacle 회피**: Beat Saber 스타일 장애물

### 🎵 BeatMap 시스템

- **자동 생성**: BPM 기반 프로시저럴 생성
- **수동 제작**: 실시간 녹음 기능
- **랜덤 생성**: 패턴 랜더마이저
- **난이도 조절**: Easy / Normal / Hard

### 📊 스코어 시스템

- 콤보 배수 적용 점수 계산
- 정확도 측정 (Perfect/Good 비율)
- 등급 판정 (SS/S/A/B/C/D)
- 결과 화면 애니메이션

---

## 🛠️ 기술 스택

| Category | Technology |
|----------|-----------|
| Engine | Unity 2021.3 LTS |
| Platform | Meta Quest 2/3 |
| SDK | Meta XR All-in-One SDK |
| Language | C# |
| Build | Android (ARM64) |

---

## 📂 프로젝트 구조
```
SamulVerse_Geobukseon/
├── Assets/
│   ├── Scenes/
│   │   └── HandTracking_Main.unity
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── RhythmGameManager.cs
│   │   │   ├── MusicManager.cs
│   │   │   └── ComboSystem.cs
│   │   ├── Gameplay/
│   │   │   ├── Note.cs
│   │   │   ├── Obstacle.cs
│   │   │   └── DrumHit.cs
│   │   └── BeatMap/
│   │       ├── BeatMapSpawner.cs
│   │       ├── BeatMapCreator.cs
│   │       └── ProceduralBeatMapGenerator.cs
│   ├── Prefabs/
│   └── Resources/
│       └── BeatMaps/
├── Packages/
└── ProjectSettings/
```

---

## 🚀 설치 및 실행

### 필수 요구사항

- Unity 2021.3 LTS
- Meta XR All-in-One SDK
- Android Build Support
- Meta Quest 2 또는 3

### 설치 방법
```bash
# 레포지토리 클론
git clone https://github.com/swj0120-ship-it/SamulVerse_Geobukseon.git

# Unity Hub에서 프로젝트 열기
# Unity 버전: 2021.3 LTS 선택
```

### 빌드 방법

1. **File > Build Settings**
2. **Platform: Android 선택**
3. **Texture Compression: ASTC**
4. **Quest 헤드셋 연결 (개발자 모드)**
5. **Build and Run**

---

## 🎯 현재 개발 현황

### ✅ 완료

- [x] 4-drum VR 레이아웃
- [x] 북채 컨트롤러 시스템
- [x] 노트 생성 및 이동
- [x] 판정 시스템 (Perfect/Good/Miss)
- [x] 콤보 시스템
- [x] 스코어 및 정확도 계산
- [x] 결과 화면
- [x] Obstacle 시스템
- [x] BeatMap 생성 도구 3종
- [x] 거북선 모델 통합

### 🔄 진행 중

- [ ] 파티클 시스템 최적화
- [ ] 10곡 BeatMap 제작
- [ ] 사운드 디자인
- [ ] UI/UX 개선

### 📋 예정

- [ ] 튜토리얼 시스템
- [ ] 곡 선택 메뉴
- [ ] 난이도 밸런싱

---

## 👥 팀 정보

**팀명**: 삼무파탈 (三無破達)

**소속**: MBC Academy Digital Twin 5기

**개발 기간**: 2025.12.19 ~ 2026.02.13

**팀 구성**: 7명

---

## 🐛 알려진 이슈

1. **ComboSystem 파티클 문제** (임시 비활성화)
   - 콤보 파티클이 노트 생성 시 함께 생성됨
   - 추후 수정 예정

2. **Android 최적화**
   - Quest 2 프레임 드롭 간헐적 발생
   - 텍스처 최적화 진행 중

---

## 📝 커밋 컨벤션
```
feat: 새로운 기능 추가
fix: 버그 수정
docs: 문서 수정
style: 코드 포맷팅
refactor: 코드 리팩토링
test: 테스트 코드
chore: 빌드, 설정 파일 수정
```

---

## 📄 라이선스

이 프로젝트는 MBC Academy Digital Twin 교육 과정의 일환으로 제작되었습니다.

---

<div align="center">

**Made with ❤️ by 삼무파탈**

</div>
```

---

## Step 2: 커밋 메시지 작성
```
페이지 하단:

"Commit changes..." 버튼 위 입력창:

Commit message:
docs: Add comprehensive README.md

Extended description (선택):
Add project overview, features, tech stack, and development status
```

---

## Step 3: 커밋!
```
우측 상단:
"Commit changes..." 버튼 클릭

팝업에서:
- Commit message 확인
- "Commit directly to the main branch" 선택
- "Commit changes" 클릭!
