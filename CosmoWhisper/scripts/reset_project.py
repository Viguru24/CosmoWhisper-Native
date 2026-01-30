import os

project_path = "/Users/louisdesouza/Documents/GitHub/CosmoWhisper-Native/CosmoWhisper/CosmoWhisper.xcodeproj/project.pbxproj"

# 1. Reset project to a "cleaner" state (or at least one we can parse)
# Since it's corrupted, we need to be careful.
with open(project_path, 'r') as f:
    lines = f.readlines()

new_lines = []
skip_until = None

for line in lines:
    if skip_until:
        if skip_until in line:
            new_lines.append(line)
            skip_until = None
        continue
    
    # Check for sections we want to replace
    if "/* Begin PBXBuildFile section */" in line:
        new_lines.append(line)
        skip_until = "/* End PBXBuildFile section */"
        # We'll add our entries here during the reconstruction
        continue
    if "/* Begin PBXFileReference section */" in line:
        new_lines.append(line)
        skip_until = "/* End PBXFileReference section */"
        continue
    # For groups and phases, we want to keep the headers but replace the lists
    new_lines.append(line)

# Now we have a project with empty sections. Let's populate them.
# Wait, this is still hard.

# Let's try this: I'll use a fixed template for the project file.
# Since I have the content from the first view_file, I'll use it.

original_file_content = """// !$*UTF8*$!
{
	archiveVersion = 1;
	classes = {
	};
	objectVersion = 56;
	objects = {

/* Begin PBXBuildFile section */
		C02500012F267FB700E4F0B0 /* AIService.swift in Sources */ = {isa = PBXBuildFile; fileRef = C02500112F267FB700E4F0B0 /* AIService.swift */; };
		C02500022F267FB700E4F0B0 /* AudioRecorder.swift in Sources */ = {isa = PBXBuildFile; fileRef = C02500212F267FB700E4F0B0 /* AudioRecorder.swift */; };
		C02500032F267FB700E4F0B0 /* ContentView.swift in Sources */ = {isa = PBXBuildFile; fileRef = C02500312F267FB700E4F0B0 /* ContentView.swift */; };
		C02500042F267FB700E4F0B0 /* CosmoWhisperApp.swift in Sources */ = {isa = PBXBuildFile; fileRef = C02500412F267FB700E4F0B0 /* CosmoWhisperApp.swift */; };
		C02500052F267FB700E4F0B0 /* InputController.swift in Sources */ = {isa = PBXBuildFile; fileRef = C02500512F267FB700E4F0B0 /* InputController.swift */; };
		C02500062F267FB700E4F0B0 /* CommandController.swift in Sources */ = {isa = PBXBuildFile; fileRef = C02500612F267FB700E4F0B0 /* CommandController.swift */; };
		C02500072F267FB700E4F0B0 /* DashboardView.swift in Sources */ = {isa = PBXBuildFile; fileRef = C02500712F267FB700E4F0B0 /* DashboardView.swift */; };
		C02500082F267FB700E4F0B0 /* WindowManager.swift in Sources */ = {isa = PBXBuildFile; fileRef = C02500812F267FB700E4F0B0 /* WindowManager.swift */; };
		C025655D2F267FB700E4F0B0 /* Assets.xcassets in Resources */ = {isa = PBXBuildFile; fileRef = C025655C2F267FB700E4F0B0 /* Assets.xcassets */; };
		C02565602F267FB700E4F0B0 /* Preview Assets.xcassets in Resources */ = {isa = PBXBuildFile; fileRef = C025655F2F267FB700E4F0B0 /* Preview Assets.xcassets */; };
/* End PBXBuildFile section */

/* Begin PBXFileReference section */
		C02500112F267FB700E4F0B0 /* AIService.swift */ = {isa = PBXFileReference; lastKnownFileType = sourcecode.swift; path = Services/AIService.swift; sourceTree = "<group>"; };
		C02500212F267FB700E4F0B0 /* AudioRecorder.swift */ = {isa = PBXFileReference; lastKnownFileType = sourcecode.swift; path = Managers/AudioRecorder.swift; sourceTree = "<group>"; };
		C02500312F267FB700E4F0B0 /* ContentView.swift */ = {isa = PBXFileReference; lastKnownFileType = sourcecode.swift; path = Views/ContentView.swift; sourceTree = "<group>"; };
		C02500412F267FB700E4F0B0 /* CosmoWhisperApp.swift */ = {isa = PBXFileReference; lastKnownFileType = sourcecode.swift; path = CosmoWhisperApp.swift; sourceTree = "<group>"; };
		C02500512F267FB700E4F0B0 /* InputController.swift */ = {isa = PBXFileReference; lastKnownFileType = sourcecode.swift; path = Managers/InputController.swift; sourceTree = "<group>"; };
		C02500612F267FB700E4F0B0 /* CommandController.swift */ = {isa = PBXFileReference; lastKnownFileType = sourcecode.swift; path = Managers/CommandController.swift; sourceTree = "<group>"; };
		C02500712F267FB700E4F0B0 /* DashboardView.swift */ = {isa = PBXFileReference; lastKnownFileType = sourcecode.swift; path = Views/DashboardView.swift; sourceTree = "<group>"; };
		C02500812F267FB700E4F0B0 /* WindowManager.swift */ = {isa = PBXFileReference; lastKnownFileType = sourcecode.swift; path = Managers/WindowManager.swift; sourceTree = "<group>"; };
		C02565552F267FB300E4F0B0 /* CosmoWhisper.app */ = {isa = PBXFileReference; explicitFileType = wrapper.application; includeInIndex = 0; path = CosmoWhisper.app; sourceTree = BUILT_PRODUCTS_DIR; };
		C025655C2F267FB700E4F0B0 /* Assets.xcassets */ = {isa = PBXFileReference; lastKnownFileType = folder.assetcatalog; path = Assets.xcassets; sourceTree = "<group>"; };
		C025655F2F267FB700E4F0B0 /* Preview Assets.xcassets */ = {isa = PBXFileReference; lastKnownFileType = folder.assetcatalog; path = "Preview Assets.xcassets"; sourceTree = "<group>"; };
		C02565612F267FB700E4F0B0 /* CosmoWhisper.entitlements */ = {isa = PBXFileReference; lastKnownFileType = text.plist.entitlements; path = CosmoWhisper.entitlements; sourceTree = "<group>"; };
/* End PBXFileReference section */

/* Begin PBXFrameworksBuildPhase section */
		C02565522F267FB300E4F0B0 /* Frameworks */ = {
			isa = PBXFrameworksBuildPhase;
			buildActionMask = 2147483647;
			files = (
			);
			runOnlyForDeploymentPostprocessing = 0;
		};
/* End PBXFrameworksBuildPhase section */

/* Begin PBXGroup section */
		C025654C2F267FB300E4F0B0 = {
			isa = PBXGroup;
			children = (
				C02565572F267FB300E4F0B0 /* CosmoWhisper */,
				C02565562F267FB300E4F0B0 /* Products */,
			);
			sourceTree = "<group>";
		};
		C02565562F267FB300E4F0B0 /* Products */ = {
			isa = PBXGroup;
			children = (
				C02565552F267FB300E4F0B0 /* CosmoWhisper.app */,
			);
			name = Products;
			sourceTree = "<group>";
		};
		C02565572F267FB300E4F0B0 /* CosmoWhisper */ = {
			isa = PBXGroup;
			children = (
				C02500112F267FB700E4F0B0 /* AIService.swift */,
				C02500212F267FB700E4F0B0 /* AudioRecorder.swift */,
				C02500312F267FB700E4F0B0 /* ContentView.swift */,
				C02500412F267FB700E4F0B0 /* CosmoWhisperApp.swift */,
				C02500512F267FB700E4F0B0 /* InputController.swift */,
				C02500612F267FB700E4F0B0 /* CommandController.swift */,
				C02500712F267FB700E4F0B0 /* DashboardView.swift */,
				C02500812F267FB700E4F0B0 /* WindowManager.swift */,
				C025655C2F267FB700E4F0B0 /* Assets.xcassets */,
				C02565612F267FB700E4F0B0 /* CosmoWhisper.entitlements */,
				C025655E2F267FB700E4F0B0 /* Preview Content */,
			);
			path = CosmoWhisper;
			sourceTree = "<group>";
		};
		C025655E2F267FB700E4F0B0 /* Preview Content */ = {
			isa = PBXGroup;
			children = (
				C025655F2F267FB700E4F0B0 /* Preview Assets.xcassets */,
			);
			path = "Preview Content";
			sourceTree = "<group>";
		};
/* End PBXGroup section */

/* Begin PBXNativeTarget section */
		C02565542F267FB300E4F0B0 /* CosmoWhisper */ = {
			isa = PBXNativeTarget;
			buildConfigurationList = C02565642F267FB700E4F0B0 /* Build configuration list for PBXNativeTarget "CosmoWhisper" */;
			buildPhases = (
				C02565512F267FB300E4F0B0 /* Sources */,
				C02565522F267FB300E4F0B0 /* Frameworks */,
				C02565532F267FB300E4F0B0 /* Resources */,
			);
			buildRules = (
			);
			dependencies = (
			);
			name = CosmoWhisper;
			productName = CosmoWhisper;
			productReference = C02565552F267FB300E4F0B0 /* CosmoWhisper.app */;
			productType = "com.apple.product-type.application";
		};
/* End PBXNativeTarget section */

/* Begin PBXProject section */
		C025654D2F267FB300E4F0B0 /* Project object */ = {
			isa = PBXProject;
			attributes = {
				BuildIndependentTargetsInParallel = 1;
				LastSwiftUpdateCheck = 1520;
				LastUpgradeCheck = 1520;
				TargetAttributes = {
					C02565542F267FB300E4F0B0 = {
						CreatedOnToolsVersion = 15.2;
					};
				};
			};
			buildConfigurationList = C02565502F267FB300E4F0B0 /* Build configuration list for PBXProject "CosmoWhisper" */;
			compatibilityVersion = "Xcode 14.0";
			developmentRegion = en;
			hasScannedForEncodings = 0;
			knownRegions = (
				en,
				Base,
			);
			mainGroup = C025654C2F267FB300E4F0B0;
			productRefGroup = C02565562F267FB300E4F0B0 /* Products */;
			projectDirPath = "";
			projectRoot = "";
			targets = (
				C02565542F267FB300E4F0B0 /* CosmoWhisper */,
			);
		};
/* End PBXProject section */

/* Begin PBXResourcesBuildPhase section */
		C02565532F267FB300E4F0B0 /* Resources */ = {
			isa = PBXResourcesBuildPhase;
			buildActionMask = 2147483647;
			files = (
				C02565602F267FB700E4F0B0 /* Preview Assets.xcassets in Resources */,
				C025655D2F267FB700E4F0B0 /* Assets.xcassets in Resources */,
			);
			runOnlyForDeploymentPostprocessing = 0;
		};
/* End PBXResourcesBuildPhase section */

/* Begin PBXSourcesBuildPhase section */
		C02565512F267FB300E4F0B0 /* Sources */ = {
			isa = PBXSourcesBuildPhase;
			buildActionMask = 2147483647;
			files = (
				C02500012F267FB700E4F0B0 /* AIService.swift in Sources */,
				C02500022F267FB700E4F0B0 /* AudioRecorder.swift in Sources */,
				C02500032F267FB700E4F0B0 /* ContentView.swift in Sources */,
				C02500042F267FB700E4F0B0 /* CosmoWhisperApp.swift in Sources */,
				C02500052F267FB700E4F0B0 /* InputController.swift in Sources */,
				C02500062F267FB700E4F0B0 /* CommandController.swift in Sources */,
				C02500072F267FB700E4F0B0 /* DashboardView.swift in Sources */,
				C02500082F267FB700E4F0B0 /* WindowManager.swift in Sources */,
			);
			runOnlyForDeploymentPostprocessing = 0;
		};
/* End PBXSourcesBuildPhase section */

/* Begin XCBuildConfiguration section */
		C02565622F267FB700E4F0B0 /* Debug */ = {
			isa = XCBuildConfiguration;
			buildSettings = {
				ALWAYS_SEARCH_USER_PATHS = NO;
				ASSETCATALOG_COMPILER_GENERATE_SWIFT_ASSET_SYMBOL_EXTENSIONS = YES;
				CLANG_ANALYZER_NONNULL = YES;
				CLANG_ANALYZER_NUMBER_OBJECT_CONVERSION = YES_AGGRESSIVE;
				CLANG_CXX_LANGUAGE_STANDARD = "gnu++20";
				CLANG_ENABLE_MODULES = YES;
				CLANG_ENABLE_OBJC_ARC = YES;
				CLANG_ENABLE_OBJC_WEAK = YES;
				CLANG_WARN_BLOCK_CAPTURE_AUTORELEASING = YES;
				CLANG_WARN_BOOL_CONVERSION = YES;
				CLANG_WARN_COMMA = YES;
				CLANG_WARN_CONSTANT_CONVERSION = YES;
				CLANG_WARN_DEPRECATED_OBJC_IMPLEMENTATIONS = YES;
				CLANG_WARN_DIRECT_OBJC_ISA_USAGE = YES_ERROR;
				CLANG_WARN_DOCUMENTATION_COMMENTS = YES;
				CLANG_WARN_EMPTY_BODY = YES;
				CLANG_WARN_ENUM_CONVERSION = YES;
				CLANG_WARN_INFINITE_RECURSION = YES;
				CLANG_WARN_INT_CONVERSION = YES;
				CLANG_WARN_NON_LITERAL_NULL_CONVERSION = YES;
				CLANG_WARN_OBJC_IMPLICIT_RETAIN_SELF = YES;
				CLANG_WARN_OBJC_LITERAL_CONVERSION = YES;
				CLANG_WARN_OBJC_ROOT_CLASS = YES_ERROR;
				CLANG_WARN_QUOTED_INCLUDE_IN_FRAMEWORK_HEADER = YES;
				CLANG_WARN_RANGE_LOOP_ANALYSIS = YES;
				CLANG_WARN_STRICT_PROTOTYPES = YES;
				CLANG_WARN_SUSPICIOUS_MOVE = YES;
				CLANG_WARN_UNGUARDED_AVAILABILITY = YES_AGGRESSIVE;
				CLANG_WARN_UNREACHABLE_CODE = YES;
				CLANG_WARN__DUPLICATE_METHOD_MATCH = YES;
				COPY_PHASE_STRIP = NO;
				DEBUG_INFORMATION_FORMAT = dwarf;
				ENABLE_STRICT_OBJC_MSGSEND = YES;
				ENABLE_TESTABILITY = YES;
				ENABLE_USER_SCRIPT_SANDBOXING = NO;
				GCC_C_LANGUAGE_STANDARD = gnu17;
				GCC_DYNAMIC_NO_PIC = NO;
				GCC_NO_COMMON_BLOCKS = YES;
				GCC_OPTIMIZATION_LEVEL = 0;
				GCC_PREPROCESSOR_DEFINITIONS = (
					"DEBUG=1",
					"$(inherited)",
				);
				GCC_WARN_64_TO_32_BIT_CONVERSION = YES;
				GCC_WARN_ABOUT_RETURN_TYPE = YES_ERROR;
				GCC_WARN_UNDECLARED_SELECTOR = YES;
				GCC_WARN_UNINITIALIZED_AUTOS = YES_AGGRESSIVE;
				GCC_WARN_UNUSED_FUNCTION = YES;
				GCC_WARN_UNUSED_VARIABLE = YES;
				LOCALIZATION_PREFERS_STRING_CATALOGS = YES;
				MACOSX_DEPLOYMENT_TARGET = 13.7;
				MTL_ENABLE_DEBUG_INFO = INCLUDE_SOURCE;
				MTL_FAST_MATH = YES;
				ONLY_ACTIVE_ARCH = YES;
				SDKROOT = macosx;
				SWIFT_ACTIVE_COMPILATION_CONDITIONS = "DEBUG $(inherited)";
				SWIFT_OPTIMIZATION_LEVEL = "-Onone";
			};
			name = Debug;
		};
		C02565632F267FB700E4F0B0 /* Release */ = {
			isa = XCBuildConfiguration;
			buildSettings = {
				ALWAYS_SEARCH_USER_PATHS = NO;
				ASSETCATALOG_COMPILER_GENERATE_SWIFT_ASSET_SYMBOL_EXTENSIONS = YES;
				CLANG_ANALYZER_NONNULL = YES;
				CLANG_ANALYZER_NUMBER_OBJECT_CONVERSION = YES_AGGRESSIVE;
				CLANG_CXX_LANGUAGE_STANDARD = "gnu++20";
				CLANG_ENABLE_MODULES = YES;
				CLANG_ENABLE_OBJC_ARC = YES;
				CLANG_ENABLE_OBJC_WEAK = YES;
				CLANG_WARN_BLOCK_CAPTURE_AUTORELEASING = YES;
				CLANG_WARN_BOOL_CONVERSION = YES;
				CLANG_WARN_COMMA = YES;
				CLANG_WARN_CONSTANT_CONVERSION = YES;
				CLANG_WARN_DEPRECATED_OBJC_IMPLEMENTATIONS = YES;
				CLANG_WARN_DIRECT_OBJC_ISA_USAGE = YES_ERROR;
				CLANG_WARN_DOCUMENTATION_COMMENTS = YES;
				CLANG_WARN_EMPTY_BODY = YES;
				CLANG_WARN_ENUM_CONVERSION = YES;
				CLANG_WARN_INFINITE_RECURSION = YES;
				CLANG_WARN_INT_CONVERSION = YES;
				CLANG_WARN_NON_LITERAL_NULL_CONVERSION = YES;
				CLANG_WARN_OBJC_IMPLICIT_RETAIN_SELF = YES;
				CLANG_WARN_OBJC_LITERAL_CONVERSION = YES;
				CLANG_WARN_OBJC_ROOT_CLASS = YES_ERROR;
				CLANG_WARN_QUOTED_INCLUDE_IN_FRAMEWORK_HEADER = YES;
				CLANG_WARN_RANGE_LOOP_ANALYSIS = YES;
				CLANG_WARN_STRICT_PROTOTYPES = YES;
				CLANG_WARN_SUSPICIOUS_MOVE = YES;
				CLANG_WARN_UNGUARDED_AVAILABILITY = YES_AGGRESSIVE;
				CLANG_WARN_UNREACHABLE_CODE = YES;
				CLANG_WARN__DUPLICATE_METHOD_MATCH = YES;
				COPY_PHASE_STRIP = NO;
				DEBUG_INFORMATION_FORMAT = "dwarf-with-dsym";
				ENABLE_NS_ASSERTIONS = NO;
				ENABLE_STRICT_OBJC_MSGSEND = YES;
				ENABLE_USER_SCRIPT_SANDBOXING = YES;
				GCC_C_LANGUAGE_STANDARD = gnu17;
				GCC_NO_COMMON_BLOCKS = YES;
				GCC_WARN_64_TO_32_BIT_CONVERSION = YES;
				GCC_WARN_ABOUT_RETURN_TYPE = YES_ERROR;
				GCC_WARN_UNDECLARED_SELECTOR = YES;
				GCC_WARN_UNINITIALIZED_AUTOS = YES_AGGRESSIVE;
				GCC_WARN_UNUSED_FUNCTION = YES;
				GCC_WARN_UNUSED_VARIABLE = YES;
				LOCALIZATION_PREFERS_STRING_CATALOGS = YES;
				MACOSX_DEPLOYMENT_TARGET = 13.7;
				MTL_ENABLE_DEBUG_INFO = NO;
				MTL_FAST_MATH = YES;
				SDKROOT = macosx;
				SWIFT_COMPILATION_MODE = wholemodule;
			};
			name = Release;
		};
		C02565652F267FB700E4F0B0 /* Debug */ = {
			isa = XCBuildConfiguration;
			buildSettings = {
				ASSETCATALOG_COMPILER_APPICON_NAME = AppIcon;
				ASSETCATALOG_COMPILER_GLOBAL_ACCENT_COLOR_NAME = AccentColor;
				CODE_SIGN_ENTITLEMENTS = CosmoWhisper/CosmoWhisper.entitlements;
				CODE_SIGN_STYLE = Automatic;
				COMBINE_HIDPI_IMAGES = YES;
				CURRENT_PROJECT_VERSION = 1;
				DEVELOPMENT_ASSET_PATHS = "\\"CosmoWhisper/Preview Content\\"";
				ENABLE_PREVIEWS = YES;
				GENERATE_INFOPLIST_FILE = YES;
				INFOPLIST_KEY_LSUIElement = YES;
				INFOPLIST_KEY_NSHumanReadableCopyright = "";
				INFOPLIST_KEY_NSMicrophoneUsageDescription = "CosmoWhisper needs microphone access to transcribe your voice.";
				INFOPLIST_KEY_NSAppleEventsUsageDescription = "CosmoWhisper needs to control other applications to paste text and perform commands.";
				LD_RUNPATH_SEARCH_PATHS = (
					"$(inherited)",
					"@executable_path/../Frameworks",
				);
				MARKETING_VERSION = 1.0;
				PRODUCT_BUNDLE_IDENTIFIER = com.cosmowhisper.CosmoWhisper;
				PRODUCT_NAME = "$(TARGET_NAME)";
				SWIFT_EMIT_LOC_STRINGS = YES;
				SWIFT_VERSION = 5.0;
			};
			name = Debug;
		};
		C02565662F267FB700E4F0B0 /* Release */ = {
			isa = XCBuildConfiguration;
			buildSettings = {
				ASSETCATALOG_COMPILER_APPICON_NAME = AppIcon;
				ASSETCATALOG_COMPILER_GLOBAL_ACCENT_COLOR_NAME = AccentColor;
				CODE_SIGN_ENTITLEMENTS = CosmoWhisper/CosmoWhisper.entitlements;
				CODE_SIGN_STYLE = Automatic;
				COMBINE_HIDPI_IMAGES = YES;
				CURRENT_PROJECT_VERSION = 1;
				DEVELOPMENT_ASSET_PATHS = "\\"CosmoWhisper/Preview Content\\"";
				ENABLE_PREVIEWS = YES;
				GENERATE_INFOPLIST_FILE = YES;
				INFOPLIST_KEY_LSUIElement = YES;
				INFOPLIST_KEY_NSHumanReadableCopyright = "";
				INFOPLIST_KEY_NSMicrophoneUsageDescription = "CosmoWhisper needs microphone access to transcribe your voice.";
				LD_RUNPATH_SEARCH_PATHS = (
					"$(inherited)",
					"@executable_path/../Frameworks",
				);
				MARKETING_VERSION = 1.0;
				PRODUCT_BUNDLE_IDENTIFIER = com.cosmowhisper.CosmoWhisper;
				PRODUCT_NAME = "$(TARGET_NAME)";
				SWIFT_EMIT_LOC_STRINGS = YES;
				SWIFT_VERSION = 5.0;
			};
			name = Release;
		};
/* End XCBuildConfiguration section */

/* Begin XCConfigurationList section */
		C02565502F267FB300E4F0B0 /* Build configuration list for PBXProject "CosmoWhisper" */ = {
			isa = XCConfigurationList;
			buildConfigurations = (
				C02565622F267FB700E4F0B0 /* Debug */,
				C02565632F267FB700E4F0B0 /* Release */,
			);
			defaultConfigurationIsVisible = 0;
			defaultConfigurationName = Release;
		};
		C02565642F267FB700E4F0B0 /* Build configuration list for PBXNativeTarget "CosmoWhisper" */ = {
			isa = XCConfigurationList;
			buildConfigurations = (
				C02565652F267FB700E4F0B0 /* Debug */,
				C02565662F267FB700E4F0B0 /* Release */,
			);
			defaultConfigurationIsVisible = 0;
			defaultConfigurationName = Release;
		};
/* End XCConfigurationList section */
	};
	rootObject = C025654D2F267FB300E4F0B0 /* Project object */;
}
"""

with open(project_path, 'w') as f:
    f.write(original_file_content)

# Now we have a clean (but flat) project file. 
# Let's use our surgical script (fixed) to add the files.
