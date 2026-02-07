# CosmoWhisper Vocabulary System - Complete Documentation

## Overview
This document explains the vocabulary system in CosmoWhisper, including the example files created and how to use them.

## Files Created

### 1. VocabularyDefaults.json
**Location:** `CosmoWhisper-Windows/CosmoWhisper/Managers/VocabularyDefaults.json`

**Purpose:** Standard default examples that ship with the application

**Contents:**
```json
{
  "TranscriptionHints": "ExampleName, example.com, company.com, specific_term",
  "Replacements": {
    "my address": "123 Example Street, City, Country",
    "my email": "user@example.com",
    "company name": "Example Corp"
  }
}
```

### 2. VocabularyExamples_Batman.json
**Location:** `CosmoWhisper-Windows/CosmoWhisper/Managers/VocabularyExamples_Batman.json`

**Purpose:** Fun Batman-themed examples for demonstrations and testing

**Contents:**
```json
{
  "TranscriptionHints": "Wayne Enterprises, Gotham City, Batcave, Alfred Pennyworth, Commissioner Gordon",
  "Replacements": {
    "my address": "1007 Mountain Drive, Gotham City, NJ 07001",
    "my email": "bruce.wayne@wayneenterprises.com",
    "my phone": "555-BATMAN",
    "my office": "Wayne Tower, 47th Floor, Gotham Financial District",
    "my assistant": "Alfred Pennyworth",
    "my company": "Wayne Enterprises",
    "my headquarters": "The Batcave, beneath Wayne Manor",
    "my vehicle": "Tumbler (Batmobile Mark VI)",
    "my partner": "Robin (Dick Grayson)",
    "my city": "Gotham City",
    "my nemesis": "The Joker",
    "my mentor": "Ra's al Ghul",
    "my tech guy": "Lucius Fox",
    "my ally": "Commissioner James Gordon",
    "my butler": "Alfred Pennyworth - The World's Greatest Butler",
    "emergency contact": "Oracle (Barbara Gordon) - 555-BIRDS",
    "backup location": "Wayne Manor East Wing Safe Room",
    "secure line": "Encrypted Bat-Signal Frequency 7.62"
  }
}
```

## Recent Fixes Applied

### 1. Fixed "Load Examples" Button Icon
- **Changed:** Question mark (?) → Lightbulb icon (💡)
- **File:** `DashboardWindow.xaml` line 2805
- **Why:** Better visual indication that this loads example data

### 2. Enhanced Whisper Hallucination Filtering
- **Added phrases:** "subtracting", "help me", "subtracting help me", "subtracting, help me"
- **File:** `TextProcessor.cs`
- **Why:** Groq's Whisper AI was adding these phrases to transcriptions even when not spoken
- **How it works:** The CleanText method now strips these common hallucinations from the end of transcriptions before processing

### 3. Intelligent Vocabulary Matching
- **Enhancement:** Vocabulary replacements now automatically strip trailing helper words (is, was, are, were, be, been, being)
- **File:** `VocabularyManager.cs`
- **Example:** 
  - You say: "my password" 
  - Whisper transcribes: "my password is"
  - Output: Exactly "sugmad24" (no "is")

### 4. Email Corrections Still Active
- **Status:** ✅ ENABLED and working
- All smart email formatting features remain active:
  - "at" → "@" conversion
  - Email spacing cleanup
  - Dot snapping for domains
  - Email lowercasing

## How the Vocabulary System Works

### Transcription Hints (Top Section)
- Comma-separated list of names, brands, technical terms
- Helps Whisper AI recognize uncommon words
- Example: "Wayne Enterprises, Gotham City, Batcave"

### Instant Corrections (Bottom Section)
- Key-value pairs for text replacement
- **Key:** What you say (e.g., "my address")
- **Value:** What gets typed (e.g., "1007 Mountain Drive, Gotham City, NJ 07001")

### Processing Order
1. **Speech → Whisper AI** → Raw transcription
2. **TextProcessor.CleanText()** → Removes hallucinations
3. **RegionalSpellingManager** → Applies US/UK spelling corrections
4. **VocabularyManager.ApplyCorrections()** → 
   - First: Email/domain formatting (if enabled)
   - Second: User vocabulary replacements (takes priority)

## User Data Location
- **Active vocabulary:** `%APPDATA%\CosmoWhisper\vocabulary.json`
- **Backup:** `%APPDATA%\CosmoWhisper\vocabulary.backup.json`

## Loading Examples
Click the "💡 Load Examples" button in the Vocabulary view to reset to default examples from `VocabularyDefaults.json`.

## Notes
- Vocabulary matching is case-insensitive
- Longer keys are matched first (prevents partial matches)
- Trailing helper words are automatically stripped
- Email corrections and vocabulary work together automatically
