# TMT-VR

TMT-VR is an immersive virtual-reality version of the Trail Making Test (Parts A and B) for the HTC VIVE Focus Vision, as a standalone headset app or as a PC VR app. Participants select numbered (Part A) or alternating number and letter (Part B) targets in order, by pointing either with their head direction or with a controller ray. Spoken instructions, a tutorial and a practice run come before each scored part. Completion times and errors are written to a file on the device that runs the app: the headset, or the PC for PC VR.

This repository holds the Unity project for release v1.1.0. The project contains four app scenes, one for each app. Each app is released twice: as an Android APK for the standalone headset, and as a Windows 64-bit build for PC VR.

| App | Scene | Pointing | Language | Motion log |
|---|---|---|---|---|
| TMT-VR Head EN | `Assets/TMTVR_Head_EN.unity` | head direction | English | off |
| TMT-VR Head GR | `Assets/TMTVR_Head_GR.unity` | head direction | Greek | off |
| TMT-VR Controller EN | `Assets/TMTVR_Controller_EN.unity` | controller ray | English | on |
| TMT-VR Controller GR | `Assets/TMTVR_Controller_GR.unity` | controller ray | Greek | on |

The four APKs and the four PC VR builds (one zip archive each) are attached to the [v1.1.0 release](../../releases/tag/v1.1.0).
- **APKs.** Each app has its own application ID, so all four can be installed on one headset.
- **PC VR builds.** Unzip a build and run its `.exe` on a Windows PC with the headset connected for PC VR (see [BUILDING.md](BUILDING.md), section 6).
- **Version labels.** The PC VR builds carry `PCVR` in their version label, for example `1.1.0 (HEAD, EN, PCVR)`, so their score files can be told apart from those of the APKs.

## Documentation

- [ARCHITECTURE.md](docs/ARCHITECTURE.md): the session flow, how the scripts connect and communicate, how pointing mode and language are selected, and the scene structure.
- [SCRIPTS.md](docs/SCRIPTS.md): what each script does, where it is used, and what it talks to.
- [VARIANTS.md](docs/VARIANTS.md): exactly how the four scenes differ.
- [DATA_OUTPUTS.md](docs/DATA_OUTPUTS.md): what the apps write, where, and in which format.
- [BUILDING.md](BUILDING.md): how to open the project, restore the package samples that are not redistributed here, build the four APKs and the four PC VR builds, and run them.

## Adding a language

The spoken instructions were synthesised with Google Cloud Text-to-Speech from the instruction script, which is included as text files (`Instructions/`). To add a language:

1. Translate the text files, keeping one file per audio clip.
2. Synthesise each file with a text-to-speech service.
3. Import the clips and assign them to the `*_lang` audio fields of `TMTVR_TestManager` in a copy of a scene, with `isEnglish` left false.
4. Translate the on-screen overlays in that scene.
5. Extend the Part B letters in `TargetTextAssigner`.

[VARIANTS.md](docs/VARIANTS.md) lists every language-dependent setting.

## Requirements

- Unity 6000.2.10f1 with Android Build Support (IL2CPP, ARM64) for the APKs, and Windows Build Support (IL2CPP) with the Visual Studio 2022 C++ build tools for the PC VR builds.
- The package versions locked in `Packages/packages-lock.json`, including OpenXR Plugin 1.15.1, XR Interaction Toolkit 3.2.1 and XR Hands 1.7.0. Do not upgrade packages if you want to reproduce the released apps.
- An HTC VIVE Focus Vision for running the apps. The PC VR builds also need a 64-bit Windows PC with an OpenXR runtime that supports the headset in PC VR.

## Scope and limitations

- TMT-VR is a research tool. It is not a medical device and does not give a diagnosis.
- The Controller apps include a passive motion logger. It records controller-ray, head-pose and selection events only. It computes no adjusted or motor-corrected score, and no such score is validated. Motion-log runs without a `run_end` record are incomplete and should be excluded from analysis.
- The source also contains eye-tracking and hand-tracking code paths. None of the four apps uses them, and they are not validated.
- The English APKs were checked visually on the headset (start-up, first spoken instruction and first instruction overlay); no validation study of the English versions is reported here.
- The PC VR builds were checked visually on the headset (TMT-VR Head EN and TMT-VR Controller EN: start-up, first spoken instruction and first instruction overlay); no validation study of the PC VR versions is reported here.

## Data and privacy

The repository contains no participant data. The APKs store results only on the headset, and the PC VR builds only on the PC, each in the app's own data folder (see [DATA_OUTPUTS.md](docs/DATA_OUTPUTS.md)). Anyone using the apps with participants is responsible for ethics approval, consent and secure handling of the files.

## Licence

- **Code** written for TMT-VR: MIT Licence, copyright © 2024–2026 National and Kapodistrian University of Athens. See [LICENSE](LICENSE).
- **Spoken instructions, on-screen instruction texts and the instruction script** (English and Greek): Creative Commons Attribution 4.0 International (CC BY 4.0). The spoken instructions were synthesised with Google Cloud Text-to-Speech.
- **Third-party components** (Unity packages and templates, fonts and other assets) keep their own licences. See [NOTICE.md](NOTICE.md), [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) and `THIRD_PARTY/`.
- **Logos and the EU emblem** are not covered by either licence. See [NOTICE.md](NOTICE.md).

## Citation

Please cite the software using [CITATION.cff](CITATION.cff) (GitHub shows a "Cite this repository" button). Releases are archived on Zenodo. Version 1.1.0: DOI [10.5281/zenodo.23751063](https://doi.org/10.5281/zenodo.23751063). All versions (resolves to the latest): DOI [10.5281/zenodo.23751062](https://doi.org/10.5281/zenodo.23751062).

## Funding

This work is part of the GERIATRIC project, funded by the European Union under the Horizon Europe Marie Skłodowska-Curie Actions (grant agreement No 101149390). Views and opinions expressed are however those of the author(s) only and do not necessarily reflect those of the European Union or the European Research Executive Agency (REA). Neither the European Union nor the granting authority can be held responsible for them.
