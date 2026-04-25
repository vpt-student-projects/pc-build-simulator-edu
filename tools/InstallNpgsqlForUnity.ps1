# Downloads Npgsql + NuGet dependencies into Assets/Plugins/Npgsql for Unity.
# Run from repo root: powershell -ExecutionPolicy Bypass -File tools/InstallNpgsqlForUnity.ps1

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$outDir = Join-Path $repoRoot "Assets\Plugins\Npgsql"
$workRoot = Join-Path $env:TEMP "unity_npgsql_install"
$nugetBase = "https://www.nuget.org/api/v2/package"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Remove-Item -Recurse -Force $workRoot -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $workRoot | Out-Null

function Get-BestDependencyGroup([System.Xml.XmlElement[]]$groups) {
    if (-not $groups) { return $null }
    $order = @(".NETStandard2.0", ".NETStandard2.1", ".NETFramework4.6.1", "net6.0", ".NETCoreApp3.1", "net5.0")
    foreach ($tf in $order) {
        $g = $groups | Where-Object { $_.targetFramework -eq $tf } | Select-Object -First 1
        if ($g) { return $g }
    }
    return $groups | Select-Object -First 1
}

function Copy-PackageLibDlls([string]$extractRoot, [string]$destDir, [string]$packageId) {
    # Unity API is .NET Standard 2.1: full netstandard2.0 Microsoft.Bcl.HashCode defines HashCode and
    # clashes with System.HashCode (CS0433 in URP). The netstandard2.1 build is a type-forwarding shim.
    if ($packageId -eq "Microsoft.Bcl.HashCode") {
        $paths = @(
            (Join-Path $extractRoot "lib\netstandard2.1"),
            (Join-Path $extractRoot "lib\netcoreapp2.1"),
            (Join-Path $extractRoot "lib\netstandard2.0"),
            (Join-Path $extractRoot "lib\net461")
        )
    }
    else {
        $paths = @(
            (Join-Path $extractRoot "lib\netstandard2.0"),
            (Join-Path $extractRoot "lib\netstandard2.1"),
            (Join-Path $extractRoot "lib\net461")
        )
    }
    foreach ($lib in $paths) {
        if (Test-Path $lib) {
            $dlls = @(Get-ChildItem $lib -Filter "*.dll" -ErrorAction SilentlyContinue)
            if ($dlls.Length -gt 0) {
                foreach ($d in $dlls) {
                    Copy-Item $d.FullName -Destination (Join-Path $destDir $d.Name) -Force
                    Write-Host "  copied $($d.Name) from $($lib.Substring($extractRoot.Length))"
                }
                return
            }
        }
    }
}

function Install-OnePackage([string]$id, [string]$version) {
    $safeId = $id.ToLowerInvariant()
    $pkgDir = Join-Path $workRoot "$safeId.$version"
    if (Test-Path $pkgDir) { return }

    New-Item -ItemType Directory -Force -Path $pkgDir | Out-Null
    $zipPath = Join-Path $pkgDir "pkg.zip"
    $url = "$nugetBase/$id/$version"
    Write-Host "Download $id $version"
    Invoke-WebRequest -Uri $url -OutFile $zipPath
    Expand-Archive -Path $zipPath -DestinationPath $pkgDir -Force

    $nuspec = Get-ChildItem $pkgDir -Filter "*.nuspec" -Recurse | Select-Object -First 1
    if (-not $nuspec) { Write-Error "No nuspec in $id $version" }

    [xml]$spec = Get-Content $nuspec.FullName
    $meta = $spec.package.metadata
    $depNode = $meta.dependencies
    $groups = @()
    if ($depNode -and $depNode.group) {
        foreach ($g in @($depNode.group)) { $groups += $g }
    }

    $chosen = Get-BestDependencyGroup $groups
    if ($chosen -and $chosen.dependency) {
        foreach ($dep in @($chosen.dependency)) {
            $depId = $dep.id
            $depVer = $dep.version
            if (-not $depId -or -not $depVer) { continue }
            if ($depVer -match "[\[\(]") {
                Write-Warning "Skipping complex range $depId $depVer (add manually if needed)"
                continue
            }
            $key = "$depId|$depVer".ToLowerInvariant()
            if (-not $script:seen.ContainsKey($key)) {
                $script:seen[$key] = $true
                $script:queue.Enqueue(@($depId, $depVer))
            }
        }
    }

    Copy-PackageLibDlls $pkgDir $outDir $id
}

$seen = @{}
$queue = [System.Collections.Queue]::new()
$rootKey = "npgsql|6.0.11"
$seen[$rootKey] = $true
$queue.Enqueue(@("Npgsql", "6.0.11"))

while ($queue.Count -gt 0) {
    $item = $queue.Dequeue()
    Install-OnePackage $item[0] $item[1]
}

function Copy-PinnedDll([string]$packageId, [string]$version, [string]$dllName) {
    $dir = Join-Path $workRoot "$($packageId.ToLowerInvariant()).$version"
    $src = Join-Path (Join-Path $dir "lib\netstandard2.0") $dllName
    if (-not (Test-Path $src)) {
        Write-Error "Pin failed: missing $src"
    }
    Copy-Item $src (Join-Path $outDir $dllName) -Force
    Write-Host "Pinned $dllName from $packageId $version"
}

# Transitive graph can pull older BCL packages; Npgsql / 6.x stack need these builds.
Copy-PinnedDll "System.Runtime.CompilerServices.Unsafe" "6.0.0" "System.Runtime.CompilerServices.Unsafe.dll"
Copy-PinnedDll "System.Numerics.Vectors" "4.5.0" "System.Numerics.Vectors.dll"

Write-Host "Done. DLLs in: $outDir"
Write-Host "Restart Unity. If a reference is still missing, run tools/ProbePackageNuspec.ps1 for that package version."
