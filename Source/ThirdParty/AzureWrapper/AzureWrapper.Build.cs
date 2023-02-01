// Author: Lucas Vilas-Boas
// Year: 2022
// Repo: https://github.com/lucoiso/UEAzSpeech

using System;
using System.IO;
using System.Collections.Generic;
using UnrealBuildTool;

public class AzureWrapper : ModuleRules
{
	private bool isArmArch()
	{
		return Target.Architecture.ToLower().Contains("arm") || Target.Architecture.ToLower().Contains("aarch");
	}

	private string GetPlatformLibsDirectory()
	{
		if (Target.Platform == UnrealTargetPlatform.Win64)
		{
			return Path.Combine(ModuleDirectory, "libs", "Win");
		}

		if (Target.Platform == UnrealTargetPlatform.Android)
		{
			return Path.Combine(ModuleDirectory, "libs", "Android");
		}

		if (Target.Platform == UnrealTargetPlatform.IOS)
		{
			return Path.Combine(ModuleDirectory, "libs", "iOS");
		}

		string ArchSubDir = isArmArch() ? "Arm64" : "x64";

		if (Target.Platform == UnrealTargetPlatform.HoloLens)
		{
			return Path.Combine(ModuleDirectory, "libs", "HoloLens", ArchSubDir);
		}

		if (Target.Platform == UnrealTargetPlatform.Mac)
		{
			return Path.Combine(ModuleDirectory, "libs", "Mac", ArchSubDir);
		}

		if (Target.Platform.ToString().ToLower().Contains("linux"))
		{
			return Path.Combine(ModuleDirectory, "libs", "Linux", ArchSubDir);
		}

		return "UNDEFINED_DIRECTORY";
	}

	private List<string> GetLibsList()
	{
		List<string> Output = new List<string>();

		if (Target.Platform == UnrealTargetPlatform.Win64 || Target.Platform == UnrealTargetPlatform.HoloLens)
		{
			Output.AddRange(new[]
			{
				"Microsoft.CognitiveServices.Speech.core.dll",
				"Microsoft.CognitiveServices.Speech.extension.audio.sys.dll",
				"Microsoft.CognitiveServices.Speech.extension.kws.dll",
				"Microsoft.CognitiveServices.Speech.extension.kws.ort.dll",
				"Microsoft.CognitiveServices.Speech.extension.lu.dll",
				"Microsoft.CognitiveServices.Speech.extension.mas.dll"
			});

			if (!isArmArch())
			{
				Output.Add("Microsoft.CognitiveServices.Speech.extension.codec.dll");
			}
		}

		else if (Target.Platform == UnrealTargetPlatform.Android || Target.Platform.ToString().ToLower().Contains("linux"))
		{
			Output.AddRange(new[]
			{
				"libMicrosoft.CognitiveServices.Speech.core.so",
				"libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so",
				"libMicrosoft.CognitiveServices.Speech.extension.kws.so",
				"libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so",
				"libMicrosoft.CognitiveServices.Speech.extension.lu.so"
			});

			if (Target.Platform.ToString().ToLower().Contains("linux"))
			{
				Output.AddRange(new[]
				{
					"libMicrosoft.CognitiveServices.Speech.extension.codec.so",
					"libMicrosoft.CognitiveServices.Speech.extension.mas.so"
				});
			}
		}

		else if (Target.Platform == UnrealTargetPlatform.IOS)
		{
			Output.Add("libMicrosoft.CognitiveServices.Speech.core.a");
		}

		else if (Target.Platform == UnrealTargetPlatform.Mac)
		{
			Output.Add("libMicrosoft.CognitiveServices.Speech.core.dylib");
		}

		return Output;
	}

	private void LinkDependenciesList(string SubDirectory, bool bAddAsPublicAdditionalLib, bool bAddAsRuntimeDependency, bool bDelayLoadDLL)
	{
		foreach (string Lib in GetLibsList())
		{
			string Dependency = Path.Combine(GetPlatformLibsDirectory(), SubDirectory, Lib);

			if (bAddAsPublicAdditionalLib)
			{
				PublicAdditionalLibraries.Add(Dependency);
			}

			if (bDelayLoadDLL)
			{
				PublicDelayLoadDLLs.Add(Lib);
			}

			if (bAddAsRuntimeDependency)
			{
				RuntimeDependencies.Add(Path.Combine(GetBinariesDirectory(), Lib), Dependency);
			}
		}
	}

	private string GetBinariesSubDirectory()
	{
		return Path.Combine("Binaries", Target.Platform.ToString(), Target.Architecture);
	}

	private string GetBinariesDirectory()
	{
		return Path.Combine(PluginDirectory, GetBinariesSubDirectory());
	}

	private void LinkSingleStaticDependency(string Directory, string Filename)
	{
		PublicAdditionalLibraries.Add(Path.Combine(Directory, Filename));
	}

	private void DefineDependenciesSubDirectory()
	{
		if (Target.Platform == UnrealTargetPlatform.Win64 || Target.Platform == UnrealTargetPlatform.HoloLens)
		{
			PublicDefinitions.Add(string.Format("AZSPEECH_BINARIES_SUBDIRECTORY=\"{0}\"", GetBinariesSubDirectory().Replace(@"\", @"\\")));
		}
		else if (Target.Platform == UnrealTargetPlatform.Mac || Target.Platform.ToString().ToLower().Contains("linux"))
		{
			PublicDefinitions.Add(string.Format("AZSPEECH_BINARIES_SUBDIRECTORY=\"{0}\"", GetBinariesSubDirectory().Replace(@"\", @"/")));
		}
	}

	private void DefineWhitelistedDependencies()
	{
		if (Target.Platform == UnrealTargetPlatform.Win64 || Target.Platform == UnrealTargetPlatform.HoloLens ||
			Target.Platform == UnrealTargetPlatform.Mac || Target.Platform.ToString().ToLower().Contains("linux"))
		{
			PublicDefinitions.Add(string.Format("AZSPEECH_WHITELISTED_BINARIES=\"{0}\"", string.Join(";", GetLibsList())));
		}
	}

	public AzureWrapper(ReadOnlyTargetRules Target) : base(Target)
	{
		Type = ModuleType.External;
		bEnableExceptions = true;

		PublicIncludePaths.AddRange(new[]
		{
			Path.Combine(ModuleDirectory, "include", "c_api"),
			Path.Combine(ModuleDirectory, "include", "cxx_api")
		});

		Console.WriteLine("AzSpeech: Initializing build for target: Platform: " + Target.Platform.ToString() + "; Architecture: " + Target.Architecture.ToString() + ";");
		Console.WriteLine("AzSpeech: Getting plugin dependencies in directory: \"" + GetPlatformLibsDirectory() + "\"");

		DefineDependenciesSubDirectory();
		DefineWhitelistedDependencies();

		if (Target.Platform == UnrealTargetPlatform.Win64 || Target.Platform == UnrealTargetPlatform.HoloLens)
		{
			LinkSingleStaticDependency(GetPlatformLibsDirectory(), "Microsoft.CognitiveServices.Speech.core.lib");
			LinkDependenciesList("Runtime", false, true, true);
		}
		else if (Target.Platform == UnrealTargetPlatform.Mac)
		{
			// Experimental UPL usage for MacOS to add the required PList data
			AdditionalPropertiesForReceipt.Add("IOSPlugin", Path.Combine(ModuleDirectory, "AzSpeech_UPL_MacOS.xml"));

			LinkSingleStaticDependency(GetPlatformLibsDirectory(), "libMicrosoft.CognitiveServices.Speech.core.a");
			LinkDependenciesList("Runtime", false, true, true);
		}
		else if (Target.Platform.ToString().ToLower().Contains("linux"))
		{
			LinkDependenciesList("", true, true, true);
		}
		else if (Target.Platform == UnrealTargetPlatform.IOS)
		{
			AdditionalPropertiesForReceipt.Add("IOSPlugin", Path.Combine(ModuleDirectory, "AzSpeech_UPL_IOS.xml"));

			foreach (string Lib in GetLibsList())
			{
				LinkSingleStaticDependency(GetPlatformLibsDirectory(), Lib);
			}
		}
		else if (Target.Platform == UnrealTargetPlatform.Android)
		{
			AdditionalPropertiesForReceipt.Add("AndroidPlugin", Path.Combine(ModuleDirectory, "AzSpeech_UPL_Android.xml"));

			// Linking both architectures: For some reason (or bug?) Target.Architecture is always empty when building for Android ._.
			foreach (string Lib in GetLibsList())
			{
				LinkSingleStaticDependency(Path.Combine(GetPlatformLibsDirectory(), "arm64-v8a"), Lib);
				LinkSingleStaticDependency(Path.Combine(GetPlatformLibsDirectory(), "armeabi-v7a"), Lib);
			}
		}
	}
}