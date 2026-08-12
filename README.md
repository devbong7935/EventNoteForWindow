# 경조사 명부 (EventNote)

결혼식 · 장례식 같은 경조사의 **하객 명부와 경조사금**을 행사별로 정리하고, 엑셀로 내보내는 Windows 데스크톱 프로그램입니다.

축의금 봉투를 손으로 세고 수첩에 적던 일을 그대로 옮겨 놓은 화면입니다. 표에 바로 입력하고, 관계(분류)별로 자동 집계하고, 식권을 쓰는 행사라면 식대까지 차감해 남은 금액을 보여줍니다.

- .NET 8 · WPF · MVVM (CommunityToolkit.Mvvm)
- 데이터는 내 컴퓨터에만 저장됩니다. 서버도, 계정도, 인터넷 연결도 필요 없습니다.

## 화면

행사 하나를 고르면 오른쪽에 그 행사의 하객 명부와 집계가 나옵니다.

![메인 화면](docs/screenshot-main.png)

행사 정보는 별도 창에서 편집합니다. 종류를 고르면 식권 사용 여부의 기본값이 따라옵니다.

![행사 편집 창](docs/screenshot-event-editor.png)

**식권을 쓰지 않는 행사**(예: 장례식)에서는 식권 열 · 식권합계 열 · 식권 타일 · 식대 차감 타일이 화면과 엑셀에서 모두 사라집니다. 위 결혼식 화면과 비교해 보세요.

![식권을 쓰지 않는 행사](docs/screenshot-funeral.png)

> 화면에 쓰인 내용은 `samples/` 의 예시 데이터입니다. 전래동화 인물로 꾸민 가상의 행사입니다.

## 주요 기능

| 기능 | 설명 |
|---|---|
| 행사 관리 | 결혼식 · 장례식 · 돌잔치 · 환갑 · 칠순 · 팔순 · 개업식 · 기타 8종. 날짜/시간, 장소, 주최(혼주·상주), 메모 |
| 하객 명부 | 표에서 바로 입력. 이름 · 금액 · 관계 · 소속 · 식권 · 비고 |
| 한글 표기 | 금액을 입력하면 `150,000 → 십오만원` 으로 자동 변환해 옆 칸에 보여줍니다 |
| 금액 입력 | 입력하는 동안 천 단위 쉼표를 넣어 줍니다 |
| 분류별 집계 | 관계별 인원수 · 금액합계 · 식권합계를 실시간 계산 |
| 식대 차감 | 식권 단가 × 식권 매수를 총액에서 뺀 "총 (식대 차감)" 을 보여줍니다 |
| 검색 | 이름 · 소속 · 관계 · 비고 · 전체 중에서 기준을 골라 필터링 |
| 자동 저장 | 마지막 입력 후 1.5초면 저장됩니다. `Ctrl+S` 로 즉시 저장도 가능 |
| 엑셀 내보내기 | 현재 행사 하나 또는 전체 행사를 `.xlsx` 로. 엑셀이 설치돼 있지 않아도 됩니다 |
| 데이터 이사 | 전체 데이터를 `.enote` 파일 하나로 내보내고, 다른 컴퓨터에서 가져오기 |

### 단축키

| 키 | 동작 |
|---|---|
| `Ctrl+N` | 새 행사 |
| `Ctrl+E` | 현재 행사 엑셀로 내보내기 |
| `Ctrl+S` | 저장 |

## 시작하기

필요한 것: **Windows** + **.NET 8 SDK**

```bash
dotnet build EventNote.sln -c Release
```

```bash
dotnet run --project EventNote/EventNote.csproj -c Release
```

Visual Studio 에서는 `EventNote.sln` 을 열고 `EventNote` 를 시작 프로젝트로 두면 됩니다.

### 예시 데이터 넣어 보기

앱에서 **파일 → 데이터 가져오기(.enote)** 를 고르고 `samples/예시데이터.enote` 를 엽니다. 자세한 내용과 데이터를 고치는 방법은 [samples/README.md](samples/README.md) 에 있습니다.

## 데이터가 저장되는 곳

```
%APPDATA%\EventNote\events.dat
```

메뉴의 **설정 → 데이터 폴더 열기** 로 바로 갈 수 있습니다.

- 이 파일은 **AES-256-GCM** 으로 암호화되어 있습니다. 메모장이나 엑셀로 열어도 내용을 알아볼 수 없고, 중간에 손을 대면 읽을 때 검출됩니다.
- 저장은 임시 파일에 먼저 쓴 뒤 바꿔치기합니다. 저장 도중 전원이 꺼져도 기존 파일이 남습니다. 직전 내용은 `.bak` 으로 보관됩니다.
- 키는 앱 안에 심어 둔 비밀에서 파생합니다. 비밀번호를 묻지 않는 대신, **프로그램 자체를 분석하는 사람까지 막아주지는 못합니다.** 남이 봐도 곤란하지 않을 수준의 개인 기록용입니다.
- 내보내는 `.enote` 파일도 같은 형식입니다. 그래서 다른 컴퓨터의 이 프로그램에서 그대로 열립니다.

## 프로젝트 구조

XAML 은 `Themes/` 아래 `ResourceDictionary` 로만 두고, 화면 클래스는 `ControlTemplate` 을 입은 코드 파일(`.cs`)로 둡니다. `.xaml.cs` 코드 비하인드가 없는 구조입니다.

| 프로젝트 | 대상 | 역할 |
|---|---|---|
| `EventNote` | `net8.0-windows` | 진입점. DI 조립, 리소스 병합, 셸 생성 |
| `EventNote.Forms` | `net8.0-windows` | 셸 창 `MainWindow` — 메뉴와 창 외형 |
| `EventNote.Main` | `net8.0-windows` | 본문 화면과 ViewModel (`MainContent`, `EventEditorWindow`) |
| `EventNote.Support` | `net8.0-windows` | 공용 UI 부품과 테마 (`AccentButton`, `CardPanel`, `StatTile`, `MoneyTextBox`, `SearchBar` …) |
| `EventNote.Core` | `net8.0` | 모델 · 저장소 · 암호화 · 엑셀 내보내기. **WPF 에 의존하지 않습니다** |

솔루션 밖에 있는 것들:

| 경로 | 용도 |
|---|---|
| `samples/` | 예시 데이터 (`sample-data.json` → `예시데이터.enote`) |
| `tools/SampleDataBuilder/` | 위 `.enote` 를 굽는 콘솔 도구 |
| `tools/IconBuilder/` | `EventNote.ico` 생성 스크립트 |
| `Setup/` | Visual Studio Installer 프로젝트 |

### 의존 패키지

| 패키지 | 쓰는 곳 |
|---|---|
| `CommunityToolkit.Mvvm` | ViewModel (`ObservableObject`, `[RelayCommand]`) |
| `ClosedXML` | `.xlsx` 생성 |
| `Microsoft.Extensions.DependencyInjection` | 서비스 등록 |

## 빌드 메모

루트의 [`Directory.Build.props`](Directory.Build.props) 는 `OptimizeImplicitlyTriggeredBuild` 를 꺼 둡니다. 이게 없으면 Visual Studio 에서 **F5 로 시작할 때** 빌드 속도를 위해 분석기와 nullable 분석을 자동으로 건너뜁니다 ("분석기를 건너뛰어 빌드 속도를 높입니다"). 대신 F5 시작이 조금 느려집니다.

## 라이선스

[MIT](LICENSE) © 2026 devBong / 데브봉
