# L3 correction — the repeated using-directive literals

Tim approved L3 for this correction: the orchestrator made the edits directly, then obtained the required independent DRY review.

## The record correction this file exists to make

The round-five rule-phase result claimed the `using AgentGuard.CrossPlatform;` duplication had been "verified live" as eliminated. **That claim was false.** The owner constant `SharedAnalyzerSources.CrossPlatformUsing` had been threaded through most fixture builders, but five Boundaries-gate fixtures still spelled the literal by hand, and a sixth site re-spelled `using AgentGuard.Boundaries;` in a file that already declared `BoundariesUsing`. The independent DRY reviewer found them; the round's own report did not.

This is the third time a rule-phase report has claimed a completeness that a direct check contradicted. The earlier two were an assertion that rule names had been copied exactly from the contract when four were paraphrases, and a declaration that Acceptance 18 fell outside the stage while a fixture declared a compiler-error exception the requirement forbids in exact words.

## What changed

Six literals replaced by their existing owners. No new helper, no analyzer change, no assertion change, no test case added or removed.

| file | fixture | owner used |
|---|---|---|
| `OneDoorIntoCrossPlatformAnalyzerTests.cs` | `CallAdapterFactorySource` | `SharedAnalyzerSources.CrossPlatformUsing` |
| `OneDoorIntoCrossPlatformAnalyzerTests.cs` | `CallOtherCrossPlatformTypeSource` | `SharedAnalyzerSources.CrossPlatformUsing` |
| `OneDoorIntoPerOsAnalyzerTests.cs` | `CallPlatformServicesCreateSource` | `SharedAnalyzerSources.CrossPlatformUsing` |
| `OneDoorIntoPerOsAnalyzerTests.cs` | `CallOtherPerOsMemberSource` | `SharedAnalyzerSources.CrossPlatformUsing` |
| `OneDoorIntoPerOsAnalyzerTests.cs` | `callOldPlatformDoor` | `SharedAnalyzerSources.CrossPlatformUsing` |
| `EngineToBoundariesOneDoorAnalyzerTests.cs` | `everyRejectedCall` | `BoundariesUsing` |

Each raw string became an interpolated raw string using the `$$"""` form, so the braces in the fixture C# stay literal and only the `{{owner}}` hole interpolates. All six remain `const`, because both owners are constants and C# permits a constant interpolated string.

## Byte-for-byte proof, taken before the edits

A throwaway program outside the repository evaluated both the before form and the after form of all six fixtures and compared them:

```
IDENTICAL  CallAdapterFactorySource           before=E016370415AC23E5 after=E016370415AC23E5 len=122/122
IDENTICAL  CallOtherCrossPlatformTypeSource   before=551D683EAA337DC1 after=551D683EAA337DC1 len=112/112
IDENTICAL  CallPlatformServicesCreateSource   before=734C0D8E6CC9D92C after=734C0D8E6CC9D92C len=117/117
IDENTICAL  CallOtherPerOsMemberSource         before=1CC32F53C1E65AC6 after=1CC32F53C1E65AC6 len=118/118
IDENTICAL  callOldPlatformDoor                before=52B3136EC783C18F after=52B3136EC783C18F len=109/109
IDENTICAL  everyRejectedCall                  before=B97A2FE24C45AABE after=B97A2FE24C45AABE len=236/236
ALL SIX BYTE-IDENTICAL
```

The owners evaluate to exactly the replaced text: `CrossPlatformUsing` is `"using " + CrossPlatformNamespace + ";"` where `CrossPlatformNamespace` is `"AgentGuard.CrossPlatform"`, and `BoundariesUsing` is the literal `"using AgentGuard.Boundaries;"`.

## Verification after the edits

The full analyzer suite passes 600 of 600. The three affected classes pass 13, 33 and 34. The AG0041 class, untouched by this correction, still passes 81 — no independently reported case was lost.

`git diff --check` is clean. A search of the three files for the two literals returns only the `BoundariesUsing` owner definition at `EngineToBoundariesOneDoorAnalyzerTests.cs:81`; `CrossPlatformUsing`'s definition lives in `SharedAnalyzerSources.cs:440`.

The one expected AG0015 production diagnostic is unchanged as intermediate impact. No production file was touched.
