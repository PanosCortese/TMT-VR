# Third-party notices

TMT-VR is built on Unity packages, the Unity VR template, and third-party fonts and assets. These keep their own licences. This file lists the notices included in this repository; it grants no licence itself. The licences for TMT-VR's own code and content are in `LICENSE` and `NOTICE.md`.

## Included notices

| Component | Included text | Scope and conditions |
|---|---|---|
| Unity VR template 9.2.0 | `THIRD_PARTY/VR_TEMPLATE_9.2.0_LICENSE.md` | Template copyright and Unity Companion License (UCL) notice. The template assets kept in this project remain under Unity's terms. |
| Unity UI and TextMesh Pro essential resources | `THIRD_PARTY/UNITY_UI_2.0.0_LICENSE.md` | Package copyright and UCL notice. Fonts and other third-party content in these resources have their own terms (below). |
| XR Interaction Toolkit 3.2.1 | `THIRD_PARTY/XRI_3.2.1_LICENSE.md`, `THIRD_PARTY/XRI_3.2.1_THIRD_PARTY_NOTICES.md` | The Starter Assets and Hands Interaction Demo samples are included as they are in the project, with their notices. The third-party notice contains the Apache 2.0 terms for the ARCore-derived component it identifies. |
| OpenXR Plugin 1.15.1 | `THIRD_PARTY/OPENXR_1.15.1_LICENSE.md`, `THIRD_PARTY/OPENXR_1.15.1_THIRD_PARTY_NOTICES.md` | The package notice places source code under the UCL and other content under the Unity Package Distribution License (UPDL). Khronos components, and Oculus components where applicable, keep their own terms. The OpenXR sample folders are therefore not included (see below). |
| XR Hands 1.7.0 | `THIRD_PARTY/XR_HANDS_1.7.0_LICENSE.md`, `THIRD_PARTY/XR_HANDS_1.7.0_THIRD_PARTY_NOTICES.md` | The package notice specifies the UPDL. The HandVisualizer sample folder and one material derived from it are therefore not included. Copies of ten files from the sample, which the apps use, are included (see below). |
| Liberation Sans | `THIRD_PARTY/LIBERATION_SANS_OFL.txt` | The font's copyright notice and SIL Open Font License 1.1, copied unchanged from the project. The notice stays with the font and its derivatives. The font is not covered by the MIT Licence, and the licence's Reserved Font Name conditions apply. |
| Inter 3.010 | `THIRD_PARTY/INTER_3.010_OFL.txt`; the same text is beside the font in `Assets/VRTemplateAssets/Fonts/Inter/` | The upstream licence at the font's recorded commit `aca7606f2`, including its 2016–2019 copyright notice. The font's own metadata states version 3.010, "Copyright 2019 The Inter project authors" and OFL 1.1. |
| EmojiOne sample sprites (TextMesh Pro) | `THIRD_PARTY/EMOJIONE_ATTRIBUTION.txt` | The attribution supplied with TextMesh Pro's essential resources. It names the provider and refers to the provider's website for licensing terms; it does not itself state a licence. |

The full Unity terms are included as `THIRD_PARTY/UNITY_COMPANION_LICENSE_v1.4.txt` and `THIRD_PARTY/UNITY_PACKAGE_DISTRIBUTION_LICENSE_v2.1.txt`. Their text was extracted from the official pages; the whitespace normalisation is recorded in `THIRD_PARTY/PROVENANCE.json`. The package notices and the Inter licence are byte-for-byte copies.

UCL section 1 addresses distribution in connection with content authored under a valid Unity engine licence; section 5 requires the licence, copyright and applicable third-party notices to be kept. UPDL section 1 limits its distribution grant to binaries integrated with project content. Keeping a notice does not extend a distribution grant. The package third-party notices refer to further terms, including the Oculus SDK where applicable; those terms continue to apply. These statements describe the attached terms and are not legal advice.

Official sources: https://unity.com/legal/licenses/unity-companion-license ; https://unity.com/legal/licenses/unity-package-distribution-license ; https://raw.githubusercontent.com/rsms/inter/aca7606f2/LICENSE.txt . Retrieval times, source-response hashes and saved-file hashes are recorded in `THIRD_PARTY/PROVENANCE.json`.

## Package content not included

Three package sample folders, and one material derived from a package sample, are not redistributed in this repository, because their licence terms do not grant redistribution as source:

- `Assets/Samples/OpenXR Plugin/1.15.1/Controller`;
- `Assets/Samples/OpenXR Plugin/1.15.1/Controller XRInput`;
- `Assets/Samples/XR Hands/1.7.0/HandVisualizer`;
- `Blue.mat`, a material derived from the HandVisualizer sample.

`BUILDING.md` explains how to restore them from the exact package versions before building. Do not re-import the XR Interaction Toolkit samples: the copies in this repository are the ones the apps use.

## Package content included as copies

`Assets/TMTVR_Head_Geriatric/` contains copies of files from the XR Hands 1.7.0 HandVisualizer sample, which the apps use: `LeftHand.fbx`, `RightHand.fbx`, `LeftHandAndroidXR.fbx` and `RightHandAndroidXR.fbx` (unchanged), and `Joint.prefab`, `VelocityPrefab.prefab`, `Red.mat`, `Green.mat`, `White.mat` and `TransparentRed.mat` (re-saved by Unity or converted to the Universal Render Pipeline). They are Unity's content, remain under the terms in `THIRD_PARTY/XR_HANDS_1.7.0_LICENSE.md` and `THIRD_PARTY/XR_HANDS_1.7.0_THIRD_PARTY_NOTICES.md`, and are not covered by the MIT Licence.
