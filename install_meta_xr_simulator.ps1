# Meta XR Simulator 수동 설치 스크립트
# 사용법: PowerShell에서 실행하거나, ZIP 파일 경로와 버전을 지정하여 실행

param(
    [string]$ZipFilePath = "",
    [string]$Version = "81.0.865"
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Meta XR Simulator 수동 설치" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ZIP 파일 경로가 지정되지 않은 경우 다운로드 폴더에서 검색
if ([string]::IsNullOrEmpty($ZipFilePath)) {
    Write-Host "ZIP 파일 경로가 지정되지 않았습니다. 다운로드 폴더에서 검색합니다..." -ForegroundColor Yellow
    
    $possibleFiles = @(
        "$env:USERPROFILE\Downloads\meta_xr_simulator_windows_$Version.zip",
        "$env:USERPROFILE\Downloads\meta_xr_simulator_windows_81.0.865*.zip",
        "$env:USERPROFILE\Downloads\*simulator*81*.zip"
    )
    
    $found = $false
    foreach ($pattern in $possibleFiles) {
        $files = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue
        if ($files) {
            $ZipFilePath = $files[0].FullName
            $found = $true
            Write-Host "ZIP 파일을 찾았습니다: $ZipFilePath" -ForegroundColor Green
            break
        }
    }
    
    if (-not $found) {
        Write-Host ""
        Write-Host "ZIP 파일을 찾을 수 없습니다!" -ForegroundColor Red
        Write-Host ""
        Write-Host "다음 중 하나를 시도하세요:" -ForegroundColor Yellow
        Write-Host "1. ZIP 파일 경로를 직접 지정: .\install_meta_xr_simulator.ps1 -ZipFilePath 'C:\path\to\file.zip' -Version '81.0.865'" -ForegroundColor White
        Write-Host "2. ZIP 파일을 다운로드 폴더에 저장하고 버전 번호 확인" -ForegroundColor White
        Write-Host ""
        Write-Host "설치 경로: $env:LOCALAPPDATA\MetaXR\MetaXrSimulator\{Version}" -ForegroundColor Cyan
        exit 1
    }
}

# ZIP 파일 존재 확인
if (-not (Test-Path $ZipFilePath)) {
    Write-Host "오류: ZIP 파일을 찾을 수 없습니다: $ZipFilePath" -ForegroundColor Red
    exit 1
}

Write-Host "ZIP 파일: $ZipFilePath" -ForegroundColor White
Write-Host "버전: $Version" -ForegroundColor White

# 설치 경로 설정
$installDir = Join-Path $env:LOCALAPPDATA "MetaXR\MetaXrSimulator\$Version"
Write-Host "설치 경로: $installDir" -ForegroundColor White
Write-Host ""

# 설치 디렉토리 생성
Write-Host "설치 디렉토리 생성 중..." -ForegroundColor Yellow
try {
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
    Write-Host "설치 디렉토리 생성 완료" -ForegroundColor Green
} catch {
    Write-Host "오류: 설치 디렉토리를 생성할 수 없습니다: $_" -ForegroundColor Red
    exit 1
}

# 기존 설치가 있으면 백업 (선택사항)
if ((Get-ChildItem $installDir -ErrorAction SilentlyContinue)) {
    Write-Host "경고: 설치 디렉토리에 기존 파일이 있습니다. 덮어씁니다..." -ForegroundColor Yellow
}

# ZIP 파일 압축 해제
Write-Host ""
Write-Host "ZIP 파일 압축 해제 중..." -ForegroundColor Yellow
try {
    Expand-Archive -Path $ZipFilePath -DestinationPath $installDir -Force
    Write-Host "압축 해제 완료" -ForegroundColor Green
} catch {
    Write-Host "오류: ZIP 파일 압축 해제 실패: $_" -ForegroundColor Red
    exit 1
}

# 설치 확인
Write-Host ""
Write-Host "설치 확인 중..." -ForegroundColor Yellow
$dllFile = Join-Path $installDir "SIMULATOR.dll"
$jsonFile = Join-Path $installDir "meta_openxr_simulator.json"

$allGood = $true

if (Test-Path $dllFile) {
    Write-Host "[OK] SIMULATOR.dll 확인 완료" -ForegroundColor Green
} else {
    Write-Host "[경고] SIMULATOR.dll을 찾을 수 없습니다" -ForegroundColor Yellow
    $allGood = $false
}

if (Test-Path $jsonFile) {
    Write-Host "[OK] meta_openxr_simulator.json 확인 완료" -ForegroundColor Green
} else {
    Write-Host "[경고] meta_openxr_simulator.json을 찾을 수 없습니다" -ForegroundColor Yellow
    $allGood = $false
}

# 설치된 파일 목록 표시
Write-Host ""
Write-Host "설치된 파일 목록:" -ForegroundColor Cyan
Get-ChildItem $installDir -File | Select-Object Name, Length | Format-Table -AutoSize

if ($allGood) {
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "설치 완료!" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "다음 단계:" -ForegroundColor Cyan
    Write-Host "1. Unity 에디터를 열거나 재시작하세요" -ForegroundColor White
    Write-Host "2. Meta > Meta XR Simulator 메뉴를 선택하세요" -ForegroundColor White
    Write-Host "3. Settings 창에서 버전 '$Version'이 설치되어 있는지 확인하세요" -ForegroundColor White
    Write-Host "4. 'Selected Version' 드롭다운에서 '$Version'을 선택하세요" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Yellow
    Write-Host "설치 완료 (일부 파일 누락 가능)" -ForegroundColor Yellow
    Write-Host "============================================" -ForegroundColor Yellow
    Write-Host "필수 파일이 누락되었을 수 있습니다. Unity에서 확인하세요." -ForegroundColor Yellow
    Write-Host ""
}
