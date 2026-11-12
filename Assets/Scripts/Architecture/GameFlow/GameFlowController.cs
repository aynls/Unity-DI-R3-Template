using System;
using System.Threading;
using Architecture.Audio;
using Architecture.Data.Settings;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using VContainer;

namespace Architecture
{
  public sealed class GameFlowController : MonoBehaviour
  {
    [Inject] private AudioSettingsBinding _audioSettingsBinding;
    [Inject] private DisplaySettingsApplier _displaySettingsApplier;
    [Inject] private ILanguageService _language;
    [Inject] private LocalizationSettingsBinding _localizationSettingsBinding;
    [Inject] private IPageNavigator _navigator;
    [Inject] private ISettingsService _settings;

    private void Start()
    {
      RunStartupAsync(this.GetCancellationTokenOnDestroy())
        .Forget();
    }

    private async UniTask RunStartupAsync(CancellationToken cancellationToken)
    {
      try
      {
        var settingsInitialization = await _settings.InitializeAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (!settingsInitialization.IsSuccess)
          Debug.LogWarning(
            "[GameFlow] Settings persistence unavailable; continuing with in-memory defaults. " +
            settingsInitialization.Error);

        var languageInitialization = await _language.InitializeAsync(cancellationToken);
        if (!languageInitialization.IsSuccess)
        {
          AbortStartup("initialize localization", languageInitialization);
          return;
        }

        var localeApplication = await _localizationSettingsBinding.ApplyCurrentAsync(cancellationToken);
        if (!localeApplication.IsSuccess)
        {
          AbortStartup("apply saved locale", localeApplication);
          return;
        }

        _displaySettingsApplier.ApplyCurrent();
        _audioSettingsBinding.ApplyCurrent();
        var route = _settings.IsFirstLaunch
          ? await _navigator.PushAsync<LanguagePage>(cancellationToken)
          : await _navigator.PushAsync<MainScenePage>(cancellationToken);
        if (!(route.Status is NavigationStatus.Succeeded or NavigationStatus.AlreadyCurrent))
          Debug.LogError($"[GameFlow] Initial navigation failed: {route.Status}. {route.Error}");
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        // Scene/scope shutdown owns this cancellation; it is not a startup failure.
      }
      catch (Exception exception)
      {
        Debug.LogError($"[GameFlow] Startup failed unexpectedly.\n{exception}");
      }
    }

    private static void AbortStartup(string phase, LanguageOperationResult result)
    {
      Debug.LogError($"[GameFlow] Failed to {phase}: {result.Status}. {result.Error}");
    }
  }
}