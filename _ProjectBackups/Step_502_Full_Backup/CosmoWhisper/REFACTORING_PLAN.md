# DashboardWindow Refactoring Plan

## Current Structure Analysis
- **Total Lines:** 1,850
- **Total Methods:** 116
- **File Size:** 73.9 KB

## Identified Logical Groups

### 1. **Navigation & View Management** (~30 methods)
- Dashboard_Click, SmartCommands_Click, Microphone_Click, etc.
- ShowDashboard, ShowSmartCommands, ShowMicrophone, etc.
- SwitchToView, SetVisibility, SetButtonActive, SetButtonInactive

### 2. **Theme Management** (~5 methods)
- ThemeOcean_Click, ThemeSunset_Click, ThemeForest_Click, ThemePurple_Click
- ApplyTheme

### 3. **Vocabulary Management** (~10 methods)
- Vocabulary_Click, ShowVocabulary, InitializeVocabularyList
- AddVocabulary_Click, DeleteVocabulary_Click, EditVocabulary_Click
- SaveVocabulary_Click, CancelVocabulary_Click
- SecureMode_Click, ConfirmSecureMode_Click, CancelSecureMode_Click

### 4. **Microphone/Audio Management** (~8 methods)
- InitializeMicrophones, ComboMics_SelectionChanged
- SldSensitivity_ValueChanged, BtnCalibrate_Click
- ToggleInteractionSounds_Click, UpdateInteractionSoundsUI

### 5. **Narration/Voice Management** (~7 methods)
- InitializeVoices, ComboVoice_SelectionChanged
- SldSpeed_ValueChanged, SldVolume_ValueChanged, SldPitch_ValueChanged
- BtnPlaySample_Click

### 6. **Preferences Management** (~25 methods)
- Preferences_Click, ShowPreferences, InitializePreferences
- SldUIScale_ValueChanged, ApplyUIScale
- ToggleClipboard_Click, ToggleAutoSubmit_Click, ToggleAutoCopy_Click
- ToggleMouseButton_Click, ToggleManusAgent_Click, ToggleManusNarration_Click
- BtnBackupNow_Click, BtnRestore_Click, BtnChangeBackup_Click
- ToggleLaunchOnStartup_Click, ToggleRegionalSpelling_Click

### 7. **Intelligence/AI Management** (~5 methods)
- Intelligence_Click, ShowIntelligence
- Personality_Click, UpdatePersonalityUI
- ComboAIProvider_SelectionChanged

### 8. **Account/Auth Management** (~8 methods)
- Account_Click, ShowAccount, ShowLogin
- PerformLogin_Click, ActivateLicense_Click
- SignOut_Click, SignUp_Click
- CheckAuthStatus

### 9. **Dashboard Stats** (~2 methods)
- UpdateDashboardStats
- ResetStats_Click

### 10. **Window Management** (~8 methods)
- DashboardWindow (constructor)
- Window_Loaded, Window_MouseDown
- Close_Click, Exit_Click
- LoadPosition, SavePosition
- LogCrash

## Proposed Refactoring Structure

```
Controllers/
├── NavigationController.cs        // View switching & navigation
├── ThemeController.cs             // Theme management
├── VocabularyController.cs        // Vocabulary CRUD operations
├── AudioController.cs             // Microphone & audio settings
├── NarrationController.cs         // Voice & narration settings
├── PreferencesController.cs       // App preferences & settings
├── IntelligenceController.cs      // AI provider & personality
├── AccountController.cs           // Authentication & licensing
└── DashboardController.cs         // Dashboard stats & metrics
```

## Implementation Strategy

### Phase 1: Create Base Controller
- Create abstract `BaseViewController` with common functionality
- Access to DashboardWindow instance
- Common UI helper methods

### Phase 2: Extract Controllers (Priority Order)
1. **NavigationController** - Most used, foundational
2. **VocabularyController** - Self-contained, clear boundaries
3. **PreferencesController** - Large, complex, high impact
4. **AudioController** - Hardware-related, isolated
5. **NarrationController** - Hardware-related, isolated
6. **ThemeController** - Simple, visual
7. **IntelligenceController** - AI-specific
8. **AccountController** - Auth-specific
9. **DashboardController** - Stats & metrics

### Phase 3: Update DashboardWindow.xaml.cs
- Keep only window lifecycle methods
- Delegate to controllers
- Maintain backward compatibility

## Benefits
- **Reduced file size:** 1,850 lines → ~200-300 lines
- **Better organization:** Logical grouping by feature
- **Easier testing:** Each controller can be tested independently
- **Improved maintainability:** Changes isolated to specific controllers
- **Better code reuse:** Controllers can be used in other contexts

## Estimated Line Distribution After Refactoring

| File | Lines | Purpose |
|------|-------|---------|
| DashboardWindow.xaml.cs | ~250 | Window lifecycle, initialization |
| NavigationController.cs | ~200 | View management |
| VocabularyController.cs | ~150 | Vocabulary operations |
| PreferencesController.cs | ~350 | Settings management |
| AudioController.cs | ~120 | Microphone settings |
| NarrationController.cs | ~150 | Voice settings |
| ThemeController.cs | ~80 | Theme switching |
| IntelligenceController.cs | ~100 | AI configuration |
| AccountController.cs | ~150 | Auth & licensing |
| DashboardController.cs | ~100 | Stats & metrics |
| BaseViewController.cs | ~100 | Shared functionality |
| **TOTAL** | **~1,750** | **(100 lines saved from cleanup)** |

---

**Next Step:** Implement BaseViewController and start with NavigationController
