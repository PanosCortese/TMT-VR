# Instruction script

The spoken instructions were synthesised from these texts with Google Cloud Text-to-Speech. Each `.txt` file holds the text of one audio clip and has the same name as the clip, which is in `Assets/TMT-VR For Export/EnglishInstruction/` or `Assets/TMT-VR For Export/GreekInstructions/`. `overlays_EN.txt` and `overlays_GR.txt` hold the on-screen labels.

| Clip | Used by |
|---|---|
| `00_*_General_Instructions` | before Part A, in every app |
| `02_*_Tutorial_A_Head`, `07_*_Tutorial_B_Head` | Head apps |
| `03_*_Tutorial_A_Pointer`, `08_*_Tutorial_B_Pointer` | Controller apps |
| `04_*_Practice_A`, `05_*_Actual_A_Start`, `09_*_Practice_B`, `10_*_Actual_B_Start` | every app |
| `01_*_Tutorial_A_EyeTracking`, `06_*_Tutorial_B_EyeTracking` | eye-tracking mode, which none of the released apps uses |

The dominant-hand prompts (`DomHand_ENG.mp3`, `DomHand_GR.mp3`) have no text file here.

To add a language, translate each file, synthesise it with a text-to-speech service, and follow "Adding a language" in the main README.

Licence: CC BY 4.0 (see `NOTICE.md`).
