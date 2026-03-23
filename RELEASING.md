# Releasing

1. Commit your changes to the default branch.
2. Push a version tag such as `v0.1.0`.
3. GitHub Actions builds the unpackaged WinUI app, creates a Velopack installer, and publishes the assets to the matching GitHub Release.
4. Installed app builds can use the `Check for updates` button to download and apply newer releases from GitHub.

The workflow file is at `.github/workflows/release.yml`.
