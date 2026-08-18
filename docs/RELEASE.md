# 릴리스 · 자동 업데이트

## 어떻게 도는가

서버 프로그램은 없다. 정적 파일 두 개면 끝난다.

| | 어디에 | 주소가 바뀌는가 |
|---|---|---|
| 매니페스트 `version.json` | 이 저장소의 `update/` 폴더 | **절대 안 바뀜** (앱에 박혀 있음) |
| 설치 exe | GitHub Releases 자산 | 버전마다 바뀜 (매니페스트가 알려 줌) |

앱은 매니페스트 하나만 본다. 폴더 목록을 뒤지지 않는다. 목록 API 는 배포처마다 다르고
언제든 바뀔 수 있어서, 거기에 기대면 어느 날 업데이트 기능 전체가 조용히 죽는다.

매니페스트 주소는 [`ServiceModules.cs`](../EventNote/Properties/ServiceModules.cs) 의 `ManifestUrl` 하나뿐이다.

```
https://raw.githubusercontent.com/devbong7935/EventNoteForWindow/main/update/version.json
```

`/releases/latest` API 를 쓰지 않는 이유는 인증 없이 부르면 **IP 당 시간당 60회** 제한이
걸리기 때문이다. 회사처럼 여러 PC 가 같은 공인 IP 를 쓰면 그 한도에 걸려 업데이트 확인이
막힌다. `raw.githubusercontent.com` 의 정적 파일은 그 제한을 받지 않는다.

### 앱 쪽 동작

- 켜고 3초 뒤 **조용히** 한 번 확인한다. 네트워크가 없거나 매니페스트를 못 읽으면 아무 말도 하지 않는다.
- 도움말 > 업데이트 확인 으로 직접 확인하면 최신일 때도 결과를 알려 준다.
- 새 버전이 있으면 물어보고, 동의하면 받아서 **SHA-256 을 대조한 뒤** 설치 exe 를 실행하고 앱을 종료한다.
- 실행 중인 파일은 덮어쓸 수 없어서 반드시 앱이 먼저 비켜야 한다.

## 릴리스 절차

### 1. 버전 올리기

두 곳을 같은 값으로 맞춘다.

| 파일 | 무엇 | 빠뜨리면 |
|---|---|---|
| `Directory.Build.props` | `<Version>` | 새로 깔아도 앱이 자기를 옛 버전이라 여겨 **업데이트 알림이 계속 뜬다** |
| NSIS 스크립트 | 설치 exe 에 박히는 버전 | 제어판의 프로그램 목록에 옛 버전으로 남는다 |

`Directory.Build.props` 의 `<Version>` 이 자동 업데이트의 기준이다. 이것만은 반드시 맞춰야 한다.

세 자리(`1.0.1`)든 네 자리(`1.0.0.1`)든 상관없다. 빠진 자리는 0 으로 채워 비교하므로
`1.0.0` 과 `1.0.0.0` 은 같은 버전으로 본다. 화면에는 네 번째 자리를 쓸 때만 네 자리로 적는다.

### 2. 앱 게시

```bash
dotnet publish EventNote\EventNote.csproj -c Release -o publish
```

NSIS 는 이 `publish` 폴더의 내용을 담으면 된다.

> 이대로면 사용자 PC 에 **.NET 8 데스크톱 런타임**이 깔려 있어야 한다. 없는 PC 도 감당하려면
> 자체 포함으로 게시하거나, NSIS 에서 런타임 설치를 함께 처리해야 한다.
>
> ```bash
> dotnet publish EventNote\EventNote.csproj -c Release -r win-x64 --self-contained -o publish
> ```

### 3. NSIS 로 설치 exe 만들기

자동 업데이트가 돌려면 `.nsi` 가 이 세 가지를 만족해야 한다.

- **덮어쓰기 설치가 되어야 한다.** 앱이 종료된 뒤 설치 exe 가 뜨므로 파일 잠금은 없다.
  기존 버전을 지우고 깔든 위에 덮든 상관없지만, 설치가 중간에 사용자에게 되묻고 멈추면 안 된다.
- **설치 경로가 매번 같아야 한다.** 버전마다 폴더가 달라지면 바로 가기가 옛 exe 를 가리킨 채 남는다.
- **`/S` 무인 설치를 지원한다면** 매니페스트의 `arguments` 에 적는다 (아래 참고).

`%LOCALAPPDATA%` 아래에 설치하면 UAC 승인 창이 뜨지 않아 업데이트가 매끄럽다.
`Program Files` 에 설치하면 매 업데이트마다 관리자 승인 창을 거쳐야 한다.

### 4. 설치 exe 를 GitHub Releases 에 올린다

매니페스트 커밋보다 **먼저** 올린다. 순서가 뒤집히면 그 사이에 업데이트를 확인한 사용자가
아직 없는 파일을 받으러 가서 실패한다. 올린 뒤 자산 주소를 복사해 둔다.

### 5. `update/version.json` 고치기

평소 손댈 건 `version` 과 `url` 둘뿐이다.

```json
{
  "version": "1.0.0.1",
  "releasedAt": "2026-08-20",
  "mandatory": false,
  "minUpgradableFrom": "1.0.0",
  "notes": "사용자에게 보여 줄 변경 내용",
  "installer": {
    "url": "https://github.com/devbong7935/EventNoteForWindow/releases/download/v1.0.0.1/EventNote_v1.0.0.1_Setup.exe",
    "size": 0,
    "sha256": "",
    "arguments": ""
  }
}
```

| 항목 | 필수 | 규칙 |
|---|---|---|
| `version` | **예** | `Directory.Build.props` 의 `<Version>` 과 같은 값 |
| `url` | **예** | **https 여야 한다.** http 면 매니페스트째로 무시한다. `.exe` 나 `.msi` 만 받는다 |
| `size` | 아니오 | 바이트. 진행률 표시에만 쓴다. `0` 이면 서버가 알려 주는 크기를 쓴다 |
| `sha256` | 아니오 | 적으면 받은 파일과 대조한다. 비우면 건너뛴다 |
| `arguments` | 아니오 | 설치 exe 에 넘길 인자. 비우면 설치 화면이 그대로 뜬다 |

`sha256` 을 비워 둬도 **받다가 끊긴 파일은 걸러진다.** 받은 바이트 수를 서버가 알려 준
크기와 대조하기 때문이다. 해시가 추가로 막아 주는 건 "엉뚱한 파일을 릴리즈에 올린 실수"
쪽이다. 넣고 싶다면 이렇게 잰다 — 대문자로 나와도 그대로 넣으면 된다.

```bash
powershell -Command "(Get-FileHash '경로\EventNote_v1.0.0.1_Setup.exe' -Algorithm SHA256).Hash"
```

**단, 적을 거면 정확해야 한다.** 형식이 어긋난 해시(64자리가 아니거나 16진수가 아닌 글자)는
매니페스트 전체를 무시하게 만든다. 반쯤 적어 두느니 비워 두는 편이 낫다.

**`arguments` 는 처음엔 비워 두길 권한다.** `/S` 를 넣었는데 `.nsi` 가 무인 설치를 제대로
처리하지 못하면, 앱은 종료됐는데 설치는 조용히 아무것도 안 하고 끝난다. 사용자 눈에는
"업데이트하겠다고 했더니 프로그램이 그냥 꺼졌다" 로 보이고 원인을 찾기 어렵다.
직접 `설치exe /S` 를 실행해 제대로 깔리는 걸 확인한 뒤에 넣는다.

### 6. 커밋 & 푸시

```bash
git add update/version.json Directory.Build.props && git commit -m "v1.0.0.1" && git push
```

이 커밋이 "새 버전 나왔다" 는 신호다. 그래서 맨 마지막이다.

### 7. 확인

```bash
curl -s "https://raw.githubusercontent.com/devbong7935/EventNoteForWindow/main/update/version.json?t=1"
```

`?t=1` 은 CDN 캐시를 피하려고 붙인다.

## 시험하는 법

배포 전에 자기 PC 에서 확인할 수 있다. `Directory.Build.props` 의 `<Version>` 만 잠깐 한 단계
낮추고 F5 로 실행하면, 3초 뒤 업데이트 알림이 떠야 한다.

```bash
git checkout Directory.Build.props
```

확인이 끝나면 이걸로 되돌린다.

## 알아 둘 것

- **첫 자동 업데이트는 이 기능이 들어간 버전 다음부터다.** 지금 사용자가 쓰는 버전에는 업데이트
  확인이 없으므로, 이 기능이 담긴 첫 버전은 한 번 직접 받아 설치해야 한다.
- **CDN 캐시** — `raw.githubusercontent.com` 은 몇 분간 옛 내용을 물고 있다. 앱은 요청 주소에
  매번 다른 값을 붙여 이를 피한다. 브라우저로 확인할 때만 옛 내용이 보일 수 있다.
- **저장소가 공개여야 한다.** 비공개로 돌리면 `raw.githubusercontent.com` 도 릴리스 자산도
  토큰이 필요해지고, 앱에 토큰을 넣는 건 하면 안 된다.
- **서명이 없으면 SmartScreen 경고가 뜬다.** 없애려면 코드 서명 인증서가 필요하다.
- **`minUpgradableFrom`** 보다 낮은 버전에서는 자동 설치를 하지 않고 직접 받으라고 안내한다.
  설치 구조를 크게 바꾼 릴리스에서 쓴다.
- **`mandatory: true`** 는 대화상자 제목만 "필수 업데이트" 로 바뀐다. 지금은 여전히 거절할 수 있다.
- **매니페스트에 BOM 이 붙으면 안 된다.** 메모장이나 PowerShell 의 `Set-Content -Encoding utf8` 로
  저장하면 보이지 않는 3바이트가 앞에 붙는다. 앱은 이를 떼고 읽도록 해 뒀지만, 다른 도구가
  읽을 때 걸릴 수 있으니 BOM 없는 UTF-8 로 저장하는 편이 낫다.
