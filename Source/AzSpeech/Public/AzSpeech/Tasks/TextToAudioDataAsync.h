// Author: Lucas Vilas-Boas
// Year: 2022
// Repo: https://github.com/lucoiso/UEAzSpeech

#pragma once

#include "CoreMinimal.h"
#include "AzSpeech/Bases/AzSpeechAudioDataSynthesisBase.h"
#include "TextToAudioDataAsync.generated.h"

/**
 *
 */
UCLASS(NotPlaceable, Category = "AzSpeech")
class AZSPEECH_API UTextToAudioDataAsync : public UAzSpeechAudioDataSynthesisBase
{
	GENERATED_BODY()

public:
	/* Task delegate that will be called when completed */
	UPROPERTY(BlueprintAssignable, Category = "AzSpeech")
	FAudioDataSynthesisDelegate SynthesisCompleted;

	/* Creates a Text-To-AudioData task that will convert your text to a audio data stream */
	UFUNCTION(BlueprintCallable, Category = "AzSpeech", meta = (BlueprintInternalUseOnly = "true", WorldContext = "WorldContextObject"))
	static UTextToAudioDataAsync* TextToAudioData(const UObject* WorldContextObject, const FString& TextToConvert, const FString& VoiceName = "Default", const FString& LanguageId = "Default");

protected:
	virtual void OnSynthesisUpdate(const Microsoft::CognitiveServices::Speech::SpeechSynthesisEventArgs& SynthesisEventArgs) override;
	virtual void BroadcastFinalResult() override;
};