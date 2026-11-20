#!/usr/bin/env python3
"""Inert NuGet locked-restore cache-state check for public hosted runners."""
import json
import os
import shutil
import subprocess
import tempfile
import zipfile
from pathlib import Path

PACKAGE_ID = "LabCacheAtomicity"
VERSION = "1.0.0"


def make_package(path: Path, marker: str) -> None:
    nuspec = ("<?xml version=\"1.0\"?><package><metadata>"
              f"<id>{PACKAGE_ID}</id><version>{VERSION}</version>"
              "<authors>OwnedLab</authors><description>Inert cache check</description>"
              "</metadata></package>")
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr(f"{PACKAGE_ID}.nuspec", nuspec)
        archive.writestr("lib/net11.0/_._", "")
        archive.writestr("content/marker.txt", marker)


def invoke(root: Path, env: dict, args: list[str]):
    return subprocess.run([os.environ.get("DOTNET_ATOMICITY_EXE", "dotnet"), *args], cwd=root, env=env,
                          capture_output=True, text=True, timeout=120)


def main() -> None:
    with tempfile.TemporaryDirectory(prefix="nuget-atomicity-") as temp:
        root = Path(temp)
        feed = root / "feed"
        project = root / "project"
        cache = root / "cache"
        feed.mkdir()
        project.mkdir()
        (root / "global.json").write_text(
            '{"sdk":{"version":"11.0.100","rollForward":"disable"}}\n')
        (project / "test.csproj").write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            '<TargetFramework>net11.0</TargetFramework>'
            '<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>'
            '<NuGetAudit>false</NuGetAudit>'
            '</PropertyGroup><ItemGroup>'
            f'<PackageReference Include="{PACKAGE_ID}" Version="{VERSION}" />'
            '</ItemGroup></Project>\n')
        (root / "NuGet.Config").write_text(
            '<?xml version="1.0"?><configuration><packageSources><clear />'
            f'<add key="owned" value="{feed}" />'
            '</packageSources></configuration>\n')
        archive = feed / f"{PACKAGE_ID}.{VERSION}.nupkg"
        env = os.environ.copy()
        env["NUGET_PACKAGES"] = str(cache)
        env["DOTNET_CLI_HOME"] = str(root / "cli-home")
        env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        env["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1"
        sdk = invoke(root, env, ["--version"])
        if sdk.returncode != 0 or sdk.stdout.strip() != "11.0.100":
            raise RuntimeError("SDK 11.0.100 unavailable")
        make_package(archive, "CLEAN")
        baseline = invoke(root, env, ["restore", str(project / "test.csproj"),
                                      "--source", str(feed), "--nologo"])
        package_dir = cache / PACKAGE_ID.lower() / VERSION
        marker = package_dir / "content" / "marker.txt"
        lock = project / "packages.lock.json"
        if baseline.returncode != 0 or not lock.exists() or not marker.exists():
            raise RuntimeError("clean locked baseline failed")
        baseline_marker_ok = marker.read_text() == "CLEAN"
        shutil.rmtree(package_dir)
        make_package(archive, "CHANGED")
        changed = invoke(root, env, ["restore", str(project / "test.csproj"),
                                     "--locked-mode", "--force", "--source",
                                     str(feed), "--nologo"])
        output = changed.stdout + changed.stderr
        result = {
            "sdk_exact": sdk.stdout.strip() == "11.0.100",
            "clean_baseline": baseline_marker_ok,
            "changed_restore_rejected": changed.returncode != 0,
            "nu1403_observed": "NU1403" in output,
            "changed_file_in_cache": marker.exists() and marker.read_text() == "CHANGED",
            "completion_metadata_in_cache": (package_dir / ".nupkg.metadata").exists(),
        }
        print(json.dumps(result, sort_keys=True))


if __name__ == "__main__":
    main()
