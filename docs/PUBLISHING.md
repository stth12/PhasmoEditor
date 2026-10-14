# Uploading to the existing repository

Target: https://github.com/stth12/PhasmoEditor

The update is prepared on branch **update-v3**. No upload, merge, tag or release
has been performed by this preparation task.

## Recommended: upload the branch

From the prepared checkout:

~~~powershell
git status
git push -u origin update-v3
~~~

Open a pull request from update-v3 to main. Review the changes and wait for the
Windows workflow to pass before merging. Do not force-push main.

## Preview release

After review and merge:

1. Run scripts/prepare-release.ps1 from the merged main branch, or download the
   ZIP and checksum from the successful GitHub Actions run.
2. Create a GitHub Release with tag **v3.0.0-preview.1**, targeting that commit.
3. Mark **Set as a pre-release**.
4. Copy docs/GITHUB_RELEASE.md into the release description. Its image URLs target
   the same tag, v3.0.0-preview.1. Include docs/images in that tagged commit.
   docs/RELEASE_NOTES.md is the local preview with relative image paths.
5. Attach PhasmoEditor-3.0.0-preview.1-win-x64.zip and its .sha256 file.

Do not mark this as a compatibility-verified stable release until a fresh save
has been tested against the current game version.

## Prepared source archive

The source ZIP contains tracked source and documentation only.
It excludes .git, binaries, build output, local settings and save files.
It is a complete snapshot: uploading it over old source through the browser
does not delete obsolete files. Prefer uploading the prepared branch.

## Backup of Git history

The .bundle file preserves the prepared branch and repository history.
To recover it in another folder:

~~~powershell
git clone PhasmoEditor-update-v3.bundle PhasmoEditor-ready
cd PhasmoEditor-ready
git switch update-v3
git remote set-url origin https://github.com/stth12/PhasmoEditor.git
~~~
