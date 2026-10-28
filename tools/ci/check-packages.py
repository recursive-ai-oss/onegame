#!/usr/bin/env python3
"""Checks a folder of packed engine packages (the stand-in for nuget.org in the package smokes).

  check-packages.py <folder> <engine version>

* Every package is at the engine version, except RecursiveAI.OneGame.Navigation, which is that version plus a
  "-preview.N" suffix (it depends on a prerelease, so it cannot be stable).
* No stable package depends on a prerelease package: NuGet refuses such a package, and a stable release must not need
  a preview to work. This also keeps Navigation (and with it OnePath) out of the other 30 packages.
"""
import glob, os, re, sys, zipfile

folder, version = sys.argv[1], sys.argv[2]
PREVIEW_ONLY = {"RecursiveAI.OneGame.Navigation"}
bad = []
if not glob.glob(os.path.join(folder, "*.nupkg")):
    sys.exit(f"no packages in {folder}")
for path in sorted(glob.glob(os.path.join(folder, "*.nupkg"))):
    with zipfile.ZipFile(path) as z:
        spec = z.read([n for n in z.namelist() if n.endswith(".nuspec")][0]).decode("utf-8-sig")
    pkg_id = re.search(r"<id>([^<]+)</id>", spec).group(1)
    pkg_version = re.search(r"<version>([^<]+)</version>", spec).group(1)
    if pkg_id in PREVIEW_ONLY:
        if not re.fullmatch(re.escape(version) + r"-preview\.\d+", pkg_version):
            bad.append(f"{pkg_id} is {pkg_version}, expected {version}-preview.N")
        continue
    if pkg_version != version:
        bad.append(f"{pkg_id} is {pkg_version}, expected {version}")
    for dep, rng in re.findall(r'<dependency id="([^"]+)" version="([^"]+)"', spec):
        if "-" in rng:
            bad.append(f"stable package {pkg_id} {pkg_version} depends on prerelease {dep} {rng}")
if bad:
    print("\n".join(bad))
    sys.exit(1)
print("package versions ok; no stable package depends on a prerelease")
