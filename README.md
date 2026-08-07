IKUSIA Scaler
====

[![GitHub release][shields-latest-release]][github-latest]
[![Build Release][shields-build-release]][github-actions-release]
[![Build Listing][shields-build-listing]][github-actions-listing]
[![License: MIT][shields-license]][license]

[shields-latest-release]: https://img.shields.io/github/v/release/HugPhiluu/IKUSIA_Scaler?display_name=tag&sort=semver
[shields-build-release]: https://img.shields.io/github/actions/workflow/status/HugPhiluu/IKUSIA_Scaler/release.yml?label=Build%20Release
[shields-build-listing]: https://img.shields.io/github/actions/workflow/status/HugPhiluu/IKUSIA_Scaler/build-listing.yml?label=Build%20Listing
[shields-license]: https://img.shields.io/badge/License-MIT-green.svg

Lightweight Unity Editor utility for VRChat avatar creators.
IKUSIA Scaler quickly rescales imported outfit assets between compatible IKUSIA avatar bodies (Mizuki, Rurune, and Kaguya), with both manual menu conversion and automatic detection support.

If you run into issues or want to suggest improvements, please open an issue on [GitHub][github-issues].

---

VRChat向けの軽量なUnity Editorユーティリティです。
IKUSIA Scaler は、IKUSIA系アバター（瑞希 / ルルネ / 輝夜）間で衣装のスケール変換を素早く行えます。
手動メニュー変換と、ドロップ時の自動検出の両方に対応しています。

不具合報告や要望は [GitHub Issues][github-issues] までお願いします。

## Installation

### Option A: Add to VCC directly

[![VCC Listing](https://img.shields.io/badge/VCC%20Listing-0A84FF)](https://hugphiluu.github.io/IKUSIA_Scaler/)

### Option B: Install via Repository URL

1. Open VCC and go to Settings.
2. Open the Packages tab.
3. Click Add Repository.
4. Add this URL:

```text
https://hugphiluu.github.io/IKUSIA_Scaler/vpm.json
```

5. Open your project in VCC and install `IKUSIA Scaler` (`com.philuu.ikusia-scaler`).

## Usage

### Manual Conversion (Context Menu)

1. In Unity Hierarchy, select an outfit root GameObject.
2. Open `GameObject > IKUSIA Scaler`.
3. Choose the target conversion profile.

IKUSIA Scaler searches for an `Armature` object under your selection, applies the selected armature scale multiplier, and applies additional per-bone multipliers if required by that profile.

### Automatic Conversion on Prefab Drop

When enabled in `Window > IKUSIA Scaler Settings`, the tool watches hierarchy changes and detects newly dropped prefab roots.

It attempts to:

1. Detect outfit source avatar from prefab name and asset path hints.
2. Detect target avatar from the destination avatar root context.
3. Match a known conversion profile.
4. Apply conversion automatically when safe.

Automatic conversion can be turned off, if manual operation is preferred.

## Supported Conversion Profiles

| From | To | Armature Multiplier | Bone Adjustments |
| --- | --- | --- | --- |
| Mizuki | Rurune | `0.95` | `Neck x1.01` |
| Mizuki | Kaguya | `0.8075` | None |
| Rurune | Mizuki | `1.0526316` | None |
| Rurune | Kaguya | `0.85` | None |
| Kaguya | Mizuki | `1.23839` | `Neck x0.97015` |
| Kaguya | Rurune | `1.17647` | None |

## Safety / Validation Notes

Before conversion, IKUSIA Scaler includes guardrails:

- Warns if your selection looks like an avatar root (Animator / VRCAvatarDescriptor / PipelineManager markers).
- Warns if Armature scale is already away from near unit scale (`1,1,1`) to reduce accidental stacked scaling.

Automatic conversion runs with conservative behavior and skips unsafe cases rather than forcing dialogs.

## Settings

Open `Window > IKUSIA Scaler Settings`.

- UI language: English / Japanese.
- Automatic Conversion: enables prefab-drop auto apply flow.
- Console Logs: enables detailed auto-detection trace logs.
- Reset All Settings: clears all settings and restarts from scratch.

## Versioning and Stability Note

This package follows Semantic Versioning.

- Public API scope: IKUSIA Scaler is currently an Editor utility package and is intended for interactive Unity editor usage.
- Menu labels, settings UI wording, and auto-detection heuristics may evolve in minor/patch releases.
- Conversion profile values are treated as behaviorally significant and will be documented in release notes when changed.

For release history, see [CHANGELOG.md](Packages/com.philuu.ikusia-scaler/CHANGELOG.md) and [CHANGELOG.ja.md](Packages/com.philuu.ikusia-scaler/CHANGELOG.ja.md).

---

## Disclaimer

This Unity extension is an independent, unofficial fan-made tool.
It is not affiliated with, endorsed by, or sponsored by Ikusia / Paryi.

IKUSIA本体とは無関係の非公式ファンメイドツールです。
Ikusia / Paryi による公認・提携・スポンサー提供は一切ありません。

[github-latest]: https://github.com/HugPhiluu/IKUSIA_Scaler/releases/latest
[github-actions-release]: https://github.com/HugPhiluu/IKUSIA_Scaler/actions/workflows/release.yml
[github-actions-listing]: https://github.com/HugPhiluu/IKUSIA_Scaler/actions/workflows/build-listing.yml
[github-issues]: https://github.com/HugPhiluu/IKUSIA_Scaler/issues
[license]: https://github.com/HugPhiluu/IKUSIA_Scaler/blob/main/Packages/com.philuu.ikusia-scaler/LICENSE
