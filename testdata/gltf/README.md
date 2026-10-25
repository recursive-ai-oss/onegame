# glTF test models

Small models from the Khronos glTF-Sample-Assets repository
(<https://github.com/KhronosGroup/glTF-Sample-Assets>, commit `8f492390ddc7a2ae221a1369ef560589cf8c70fb`), copied unchanged. They are test
data for the importer only. Each folder keeps the model's own `LICENSE.md`. The larger models used by the full corpus run (Sponza,
DamagedHelmet, Duck, MetalRoughSpheres, NormalTangentTest and others) are not committed: `tools/fetch-gltf-samples.sh` downloads them
into a folder, and the tests read it from the `ONEGAME_GLTF_SAMPLES` environment variable.

| Model | Variants here | Licence | Credit |
|---|---|---|---|
| Box | glTF, glTF-Binary, glTF-Embedded | CC-BY-4.0 | © 2017 Cesium |
| AnimatedMorphCube | glTF, glTF-Binary, glTF-Quantized | CC0-1.0 | Microsoft |
| SimpleSparseAccessor | glTF, glTF-Embedded | CC-BY-4.0 | Marco Hutter |
| Fox | glTF, glTF-Binary | CC-BY-4.0 (the CC0 grant in its licence file covers part) | PixelMannen (model), tomkranis (rigging and animation), @AsoboStudio and @scurest (conversion to glTF) |
| TextureTransformTest | glTF | CC0-1.0 | Microsoft |
| EmissiveStrengthTest | glTF, glTF-Binary | CC-BY-4.0 | © 2022 AGI, Ed Mackey |
| InterpolationTest | glTF | CC0-1.0 | Khronos |

BoxTextured is left out of the committed set because its texture is a trademarked logo. CC-BY-4.0 material is redistributed under that licence with the credit above.
