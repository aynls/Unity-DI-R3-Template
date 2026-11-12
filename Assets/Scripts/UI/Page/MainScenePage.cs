using System;
using System.Threading;
using Architecture.Audio;
using Cysharp.Threading.Tasks;
using Generated;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

namespace UI
{
  public sealed class MainScenePage : BasePage
  {
    private const int IntroMilliseconds = 1000;
    private static readonly SfxCueId ClickSound = new(AudioClipName.SFX.ClickSound);
    private readonly IPageNavigator _navigator;

    private readonly ISfxPlayer _sfxPlayer;
    private IDisposable _anyButton;
    private VisualElement _menu;

    private Label _prompt;
    private bool _promptConsumed;
    private Button _settingsButton;

    public MainScenePage(ISfxPlayer sfxPlayer, IPageNavigator navigator)
    {
      _sfxPlayer = sfxPlayer;
      _navigator = navigator;
    }

    protected override void BindView()
    {
      _prompt = Element<Label>("pressAnyButton");
      _menu = Element<VisualElement>("mainMenu");
      _settingsButton = Element<Button>("settings");
      _settingsButton.clicked += OpenSettings;
    }

    public override async UniTask OnEnter(CancellationToken cancellationToken)
    {
      await RevealAsync(_prompt, IntroMilliseconds, cancellationToken);
      View.SetEnabled(true);
      WaitForAnyButton();
    }

    public override UniTask OnPause(CancellationToken cancellationToken)
    {
      StopWaiting();
      return base.OnPause(cancellationToken);
    }

    public override UniTask OnResume(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      WaitForAnyButton();
      return base.OnResume(cancellationToken);
    }

    private void WaitForAnyButton()
    {
      if (_promptConsumed || _anyButton != null) return;

      _anyButton = InputSystem.onAnyButtonPress.CallOnce(_ =>
      {
        _anyButton = null;
        _promptConsumed = true;
        PlayClickSound();
        RevealMenuAsync(LifetimeToken).Forget(ReportUnexpectedError);
      });
    }

    private async UniTask RevealMenuAsync(CancellationToken cancellationToken)
    {
      _prompt.AddToClassList("intro-exit");
      await WaitForFadeAsync(IntroMilliseconds, cancellationToken);
      _prompt.AddToClassList(HiddenClass);
      _menu.SetEnabled(false);
      await RevealAsync(_menu, IntroMilliseconds, cancellationToken);
      _menu.SetEnabled(true);
    }

    private void OpenSettings()
    {
      _settingsButton.SetEnabled(false);
      PlayClickSound();
      OpenSettingsAsync(LifetimeToken).Forget(ReportUnexpectedError);
    }

    private async UniTask OpenSettingsAsync(CancellationToken cancellationToken)
    {
      try
      {
        var result = await _navigator.PushAsync<SettingsPage>(cancellationToken);
        if (!result.IsSuccess)
          Debug.LogError($"[MainScenePage] Open settings failed: {result.Status}. {result.Error}");
      }
      finally
      {
        if (!cancellationToken.IsCancellationRequested) _settingsButton.SetEnabled(true);
      }
    }

    private void PlayClickSound()
    {
      _sfxPlayer.PlayAsync(ClickSound, cancellationToken: LifetimeToken)
        .Forget(ReportUnexpectedError);
    }

    private static void ReportUnexpectedError(Exception exception)
    {
      if (exception is not OperationCanceledException)
        Debug.LogError($"[MainScenePage] Menu action failed: {exception}");
    }

    private void StopWaiting()
    {
      _anyButton?.Dispose();
      _anyButton = null;
    }

    protected override void ReleaseView()
    {
      StopWaiting();
      if (_settingsButton != null) _settingsButton.clicked -= OpenSettings;
    }
  }
}