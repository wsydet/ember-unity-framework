"""Install, check, update, or restore the project-local upstream PPT Master skill."""

from __future__ import annotations

import argparse
from contextlib import contextmanager
from datetime import datetime, timedelta, timezone
import gzip
import hashlib
from http.client import IncompleteRead
import json
import os
from pathlib import Path
import re
import shutil
import stat
import subprocess
import sys
from tempfile import TemporaryDirectory
import time
from urllib.error import HTTPError, URLError
from urllib.parse import quote
from urllib.request import Request, urlopen


REPOSITORY = "https://github.com/hugohe3/ppt-master"
UPSTREAM_DIRECTORY = "skills/ppt-master"
API = "https://api.github.com/repos/hugohe3/ppt-master"
COMMIT = re.compile(r"[0-9a-f]{40}", re.IGNORECASE)
TAG = re.compile(r"v?(\d+)\.(\d+)\.(\d+)")
BACKUP_ID = re.compile(r"\d{8}T\d{12}Z-[0-9a-f]{12}")
PROJECT = Path(__file__).resolve().parents[2]
TOOLS = PROJECT / "Tools" / "SkillMaintenance"
SKILL = PROJECT / ".agents" / "skills" / "ppt-master"
RECORD = TOOLS / "ppt-master.source.json"
CACHE = TOOLS / ".cache"
BACKUPS = TOOLS / ".backups" / "ppt-master"


def plain_path(path: Path) -> Path:
    """Confine changes to this project and reject symlinks and Windows junctions."""
    relative = path.relative_to(PROJECT)
    current = PROJECT
    for part in relative.parts:
        if part == "..":
            raise ValueError(f"Path leaves the Unity project: {path}")
        current /= part
        try:
            info = current.lstat()
        except FileNotFoundError:
            continue
        if stat.S_ISLNK(info.st_mode) or (
            getattr(info, "st_file_attributes", 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT
        ):
            raise ValueError(f"Reparse point or symlink is not allowed: {current}")
    return path


def regular_files(root: Path):
    plain_path(root)
    def raise_walk_error(error: OSError) -> None:
        raise error

    for directory, dirs, files in os.walk(root, onerror=raise_walk_error):
        if not dirs and not files:
            raise ValueError(f"Unexpected empty directory in the skill tree: {directory}")
        for name in dirs + files:
            path = Path(directory) / name
            info = path.lstat()
            if stat.S_ISLNK(info.st_mode) or (
                getattr(info, "st_file_attributes", 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT
            ):
                raise ValueError(f"Reparse point or symlink is not allowed: {path}")
            if not (stat.S_ISDIR(info.st_mode) or stat.S_ISREG(info.st_mode)):
                raise ValueError(f"Unsupported file: {path}")
        for name in files:
            yield Path(directory) / name


def fingerprint(root: Path, expected_blobs: dict[str, str] | None = None) -> tuple[str, int]:
    entries = sorted((p.relative_to(root).as_posix(), p) for p in regular_files(root))
    if not entries:
        raise ValueError(f"Empty skill directory: {root}")
    if expected_blobs is not None and {name for name, _ in entries} != expected_blobs.keys():
        names = {name for name, _ in entries}
        missing = sorted(expected_blobs.keys() - names)
        extra = sorted(names - expected_blobs.keys())
        raise ValueError(f"Archive differs from official tree. Missing: {missing[:5]}; extra: {extra[:5]}")

    result = hashlib.sha256()
    for name, path in entries:
        content = path.read_bytes()
        if expected_blobs is not None:
            blob = hashlib.sha1(usedforsecurity=False)
            blob.update(f"blob {len(content)}\0".encode("ascii"))
            blob.update(content)
            if blob.hexdigest() != expected_blobs[name]:
                raise ValueError(f"File differs from official Git blob: {name}")
        result.update(name.encode("utf-8") + b"\0" + hashlib.sha256(content).digest())
    return result.hexdigest(), len(entries)


def api_json(path: str) -> dict:
    request = Request(
        API + "/" + path,
        headers={
            "Accept": "application/vnd.github+json",
            "Accept-Encoding": "gzip",
            "User-Agent": "Ember-PptMaster-Maintenance",
        },
    )
    for attempt in range(3):
        try:
            with urlopen(request, timeout=30) as response:
                payload = response.read()
                if response.headers.get("Content-Encoding") == "gzip":
                    payload = gzip.decompress(payload)
                return json.loads(payload)
        except (IncompleteRead, URLError, TimeoutError, EOFError, json.JSONDecodeError) as error:
            permanent = isinstance(error, HTTPError) and error.code < 500
            if permanent or attempt == 2:
                raise
            time.sleep(attempt + 1)
    raise RuntimeError("GitHub request attempts were exhausted.")


def latest_tag() -> str:
    output = subprocess.check_output(
        ["git", "ls-remote", "--tags", "--refs", REPOSITORY], text=True, timeout=45
    )
    tags = (line.split("refs/tags/", 1)[1] for line in output.splitlines() if "refs/tags/" in line)
    versions = [(tuple(map(int, match.groups())), tag) for tag in tags if (match := TAG.fullmatch(tag))]
    if not versions:
        raise ValueError("The official repository returned no stable version tags.")
    return max(versions)[1]


def resolve_commit(ref: str) -> str:
    if not (TAG.fullmatch(ref) or COMMIT.fullmatch(ref)):
        raise ValueError("Specify a stable tag (for example v6.9.0) or a full 40-character commit SHA.")
    data = api_json(f"commits/{quote(ref, safe='')}")
    sha = data.get("sha", "")
    if not COMMIT.fullmatch(sha) or (COMMIT.fullmatch(ref) and ref.lower() != sha):
        raise ValueError(f"GitHub did not resolve the requested commit: {ref}")
    return sha


def new_record(root: Path, ref: str, commit: str, expected_blobs: dict[str, str]) -> dict:
    text = (root / "SKILL.md").read_text(encoding="utf-8")
    if not text.startswith("---\n") or "\n---\n" not in text[4:]:
        raise ValueError("SKILL.md needs UTF-8 frontmatter.")
    frontmatter = text[4:].split("\n---\n", 1)[0]
    version = re.search(r"""(?m)^\s+version:\s*["']?([\w.+-]+)""", frontmatter)
    license_id = re.search(r"""(?m)^\s+license:\s*["']?([\w.+-]+)""", frontmatter)
    if not re.search(r"(?m)^name:\s*ppt-master\s*$", frontmatter) or not version or not license_id:
        raise ValueError("Could not read the official skill name or version from SKILL.md.")
    for name in ("LICENSE", "requirements.txt", "scripts", "templates", "workflows", "references"):
        if not (root / name).exists():
            raise ValueError(f"Official skill component is missing: {name}")
    digest, count = fingerprint(root, expected_blobs)
    return {
        "schema": 1,
        "repository": REPOSITORY,
        "upstreamDirectory": UPSTREAM_DIRECTORY,
        "ref": ref,
        "commit": commit,
        "version": version[1],
        "installedAt": datetime.now(timezone(timedelta(hours=8))).isoformat(timespec="seconds"),
        "fileCount": count,
        "treeSha256": digest,
        "license": license_id[1],
        "updateInstructions": "README.md",
    }


def read_record(path: Path) -> dict | None:
    plain_path(path)
    if not path.exists():
        return None
    record = json.loads(path.read_text(encoding="utf-8"))
    if (
        not isinstance(record, dict)
        or record.get("schema") != 1
        or record.get("repository") != REPOSITORY
        or record.get("upstreamDirectory") != UPSTREAM_DIRECTORY
        or not COMMIT.fullmatch(record.get("commit", ""))
        or not re.fullmatch(r"[0-9a-f]{64}", record.get("treeSha256", ""))
        or not isinstance(record.get("fileCount"), int)
        or not isinstance(record.get("ref"), str)
        or not isinstance(record.get("version"), str)
    ):
        raise ValueError(f"Invalid PPT Master source record: {path}")
    return record


def matches(root: Path, record: dict) -> bool:
    return fingerprint(root) == (record["treeSha256"], record["fileCount"])


def fetch_skill(commit: str, work: Path) -> tuple[Path, dict[str, str]]:
    checkout = work / "source"
    checkout.mkdir()

    def git(*args: str) -> bytes:
        return subprocess.check_output(
            [
                "git", "-C", str(checkout), "-c", "init.templateDir=",
                "-c", f"core.hooksPath={work / 'no-hooks'}", *args,
            ],
            timeout=180,
        )

    git("init", "--quiet")
    git("config", "core.longpaths", "true")
    git("config", "core.autocrlf", "false")
    git("remote", "add", "origin", REPOSITORY)
    git("config", "remote.origin.promisor", "true")
    git("config", "remote.origin.partialclonefilter", "blob:none")
    git("fetch", "--quiet", "--no-tags", "--depth=1", "--filter=blob:none", "origin", commit)

    prefix = UPSTREAM_DIRECTORY + "/"
    listing = git("ls-tree", "-r", "-z", "--full-tree", commit, "--", UPSTREAM_DIRECTORY)
    blobs = {}
    for entry in listing.decode("utf-8").split("\0"):
        if not entry:
            continue
        details, path = entry.split("\t", 1)
        mode, kind, sha = details.split()
        if mode not in ("100644", "100755") or kind != "blob" or not path.startswith(prefix):
            raise ValueError(f"Unsupported upstream entry: {path}")
        blobs[path[len(prefix):]] = sha
    if "SKILL.md" not in blobs:
        raise ValueError(f"Official commit {commit} does not contain {UPSTREAM_DIRECTORY}/SKILL.md.")

    git("sparse-checkout", "set", "--no-cone", "/" + prefix)
    git("checkout", "--quiet", "--detach", commit)
    if git("rev-parse", "HEAD").decode("ascii").strip() != commit:
        raise ValueError("Temporary checkout does not match the requested commit.")
    return checkout / UPSTREAM_DIRECTORY, blobs


@contextmanager
def maintenance_lock():
    plain_path(CACHE).mkdir(parents=True, exist_ok=True)
    lock = plain_path(CACHE / "ppt-master.lock")
    try:
        handle = lock.open("x", encoding="utf-8")
    except FileExistsError:
        raise ValueError(f"PPT Master maintenance is already locked: {lock}") from None
    with handle:
        handle.write(f"{os.getpid()}\n")
    try:
        yield
    finally:
        lock.unlink()


def deploy(incoming: Path, record: dict, work: Path, current: dict | None) -> Path | None:
    plain_path(SKILL)
    plain_path(RECORD)
    plain_path(incoming)
    pending = work / "source.next.json"
    pending.write_text(json.dumps(record, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    backup = None
    old_moved = False
    new_moved = False
    try:
        if read_record(RECORD) != current:
            raise ValueError("The source record changed during maintenance; run -Check again.")
        SKILL.parent.mkdir(parents=True, exist_ok=True)
        if SKILL.exists():
            if current is None or not matches(SKILL, current):
                raise ValueError(f"Local changes detected in {SKILL}; no files were replaced.")
            stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
            backup = plain_path(BACKUPS / f"{stamp}-{current['commit'][:12]}")
            backup.mkdir(parents=True)
            shutil.copyfile(RECORD, backup / RECORD.name)
            SKILL.rename(backup / "ppt-master")
            old_moved = True
        incoming.rename(SKILL)
        new_moved = True
        os.replace(pending, RECORD)
    except BaseException:
        try:
            if new_moved:
                SKILL.rename(incoming)
            if old_moved:
                (backup / "ppt-master").rename(SKILL)
        except OSError as rollback_error:
            raise RuntimeError(f"Automatic recovery failed. Keep {backup} and inspect {SKILL}.") from rollback_error
        raise
    return backup


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("check", "install", "update", "restore"))
    parser.add_argument("--ref")
    parser.add_argument("--backup")
    args = parser.parse_args()
    if not (PROJECT / "ProjectSettings" / "ProjectVersion.txt").is_file():
        raise ValueError(f"Unity project root not found: {PROJECT}")
    if args.mode == "check":
        current = read_record(RECORD)
        target = args.ref or latest_tag()
        sha = resolve_commit(target)
        if plain_path(SKILL).is_dir():
            if current is None:
                raise ValueError(f"An unmanaged skill exists: {SKILL}")
            print(f"Installed: {current['ref']} / {current['version']} / {current['commit']}")
            if not matches(SKILL, current):
                print(f"Local changes detected: {SKILL}. Update is blocked.")
                return 1
        else:
            print("PPT Master is not installed in this project.")
        print(f"Official target: {target} / {sha}")
        print("Up to date." if current and SKILL.is_dir() and current["commit"] == sha else "Target differs from the installed version.")
        return 0

    with maintenance_lock():
        current = read_record(RECORD)
        installed = plain_path(SKILL).exists()
        if args.mode == "install" and installed:
            raise ValueError(f"Installation would overwrite an existing skill: {SKILL}")
        if args.mode == "update" and not installed:
            raise ValueError("The skill directory is missing. Use -RestoreBackup or -Install explicitly.")
        if installed and (current is None or not matches(SKILL, current)):
            raise ValueError(f"Local changes or missing source record in {SKILL}; update cancelled.")

        with TemporaryDirectory(prefix="ppt-master-", dir=CACHE) as temp:
            work = plain_path(Path(temp))
            if args.mode == "restore":
                if not BACKUP_ID.fullmatch(args.backup or ""):
                    raise ValueError("Specify a backup directory name from Tools/SkillMaintenance/.backups/ppt-master.")
                backup_path = plain_path(BACKUPS / args.backup)
                record = read_record(backup_path / RECORD.name)
                source = backup_path / "ppt-master"
                if record is None or not matches(source, record):
                    raise ValueError(f"Backup is missing or its fingerprint differs: {backup_path}")
                incoming = work / "incoming"
                shutil.copytree(source, incoming)
                if not matches(incoming, record):
                    raise ValueError(f"Backup changed during staging: {backup_path}")
            else:
                pinned = current if args.mode == "install" and not args.ref else None
                ref = args.ref or (pinned["ref"] if pinned else latest_tag())
                sha = resolve_commit(pinned["commit"] if pinned else ref)
                if installed and current["commit"] == sha:
                    print(f"Already installed: {ref} / {sha}")
                    return 0
                incoming, blobs = fetch_skill(sha, work)
                record = new_record(incoming, ref, sha, blobs)
                if pinned and (record["treeSha256"], record["fileCount"]) != (
                    pinned["treeSha256"], pinned["fileCount"]
                ):
                    raise ValueError("Downloaded skill does not match the copied source record.")

            backup = deploy(incoming, record, work, current)
            print(f"Active: {record['ref']} / {record['version']} / {record['commit']}")
            if backup:
                print(f"Backup: {backup}")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, RuntimeError, ValueError, KeyError, IncompleteRead, subprocess.SubprocessError) as error:
        print(f"PPT Master: {error}", file=sys.stderr)
        sys.exit(1)
