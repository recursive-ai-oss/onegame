#!/usr/bin/env python3
"""Checks a folder of packed engine packages (the stand-in for nuget.org in the package smokes).

  check-packages.py <folder> <engine version>

* Every package is at the engine version, and that version is a stable one: no package is a prerelease.
* No package depends on a prerelease package: NuGet refuses such a package, and a release must not need a preview to work.
"""
import glob, os, re, sys, zipfile

folder, version = sys.argv[1], sys.argv[2]
bad = []
if "-" in version:
    bad.append(f"engine version {version} is a prerelease; every package must be stable")
if not glob.glob(os.path.join(folder, "*.nupkg")):
    sys.exit(f"no packages in {folder}")
for path in sorted(glob.glob(os.path.join(folder, "*.nupkg"))):
    with zipfile.ZipFile(path) as z:
        spec = z.read([n for n in z.namelist() if n.endswith(".nuspec")][0]).decode("utf-8-sig")
    pkg_id = re.search(r"<id>([^<]+)</id>", spec).group(1)
    pkg_version = re.search(r"<version>([^<]+)</version>", spec).group(1)
    if "-" in pkg_version:
        bad.append(f"{pkg_id} is {pkg_version}, a prerelease")
    if pkg_version != version:
        bad.append(f"{pkg_id} is {pkg_version}, expected {version}")
    for dep, rng in re.findall(r'<dependency id="([^"]+)" version="([^"]+)"', spec):
        if "-" in rng:
            bad.append(f"{pkg_id} {pkg_version} depends on prerelease {dep} {rng}")
if bad:
    print("\n".join(bad))
    sys.exit(1)
print("package versions ok; every package is stable and at the engine version")
