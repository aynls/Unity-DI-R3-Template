using System;
using System.Collections.Generic;
using System.Threading;
using Architecture;
using Architecture.Data.Settings;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
  public sealed class SettingsPage : BasePage
  {
    private static readonly GameResolution[] Resolutions =
      (GameResolution[])Enum.GetValues(typeof(GameResolution));

    private readonly IPageNavigator _navigator;

    private readonly ISettingsService _settings;
    private Button _finish;
    private Toggle _fullScreen;
    private SliderInt _music;
    private Label _musicValue;
    private DropdownField _resolution;
    private SliderInt _sfx;
    private Label _sfxValue;

    private DisposableBag _subscriptions;

    public SettingsPage(ISettingsService settings, IPageNavigator navigator)
    {
      _settings = settings;
      _navigator = navigator;
    }

    protected override void BindView()
    {
      _resolution = Element<DropdownField>("resolution");
      _fullScreen = Element<Toggle>("fullScreen");
      _music = Element<SliderInt>("musicVolume");
      _sfx = Element<SliderInt>("sfxVolume");
      _musicValue = Element<Label>("musicValue");
      _sfxValue = Element<Label>("sfxValue");
      _finish = Element<Button>("finish");

      var choices = new List<string>(Resolutions.Length);
      foreach (var resolution in Resolutions) choices.Add(resolution.ToString().Substring("Res_".Length));

      _resolution.choices = choices;
      Render(_settings.Current);
      _resolution.RegisterValueChangedCallback(OnResolutionChanged);
      _fullScreen.RegisterValueChangedCallback(OnWindowModeChanged);
      _music.RegisterValueChangedCallback(OnMusicVolumeChanged);
      _sfx.RegisterValueChangedCallback(OnSfxVolumeChanged);
      _finish.clicked += Close;
      _settings.Changes.Subscribe(Render).AddTo(ref _subscriptions);
    }

    public override UniTask OnResume(CancellationToken cancellationToken)
    {
      Render(_settings.Current);
      return base.OnResume(cancellationToken);
    }

    private void OnResolutionChanged(ChangeEvent<string> evt)
    {
      var index = _resolution.choices.IndexOf(evt.newValue);
      if (index >= 0) ApplyResult(_settings.SetResolution(Resolutions[index]), "resolution");
    }

    private void OnWindowModeChanged(ChangeEvent<bool> evt)
    {
      ApplyResult(
        _settings.SetWindowMode(evt.newValue ? GameWindow.FullScreenWindow : GameWindow.Window), "window mode");
    }

    private void OnMusicVolumeChanged(ChangeEvent<int> evt)
    {
      ApplyResult(_settings.SetMusicVolume(evt.newValue), "music volume");
    }

    private void OnSfxVolumeChanged(ChangeEvent<int> evt)
    {
      ApplyResult(_settings.SetSfxVolume(evt.newValue), "SFX volume");
    }

    private void ApplyResult(SettingsUpdateResult result, string setting)
    {
      if (!result.IsSuccess) Debug.LogError($"[SettingsPage] Failed to save {setting}: {result.Error}");

      Render(_settings.Current);
    }

    private void Render(SettingsSnapshot settings)
    {
      var index = Array.IndexOf(Resolutions, settings.Resolution);
      if (index >= 0) _resolution.SetValueWithoutNotify(_resolution.choices[index]);

      _fullScreen.SetValueWithoutNotify(settings.WindowMode == GameWindow.FullScreenWindow);
      _music.SetValueWithoutNotify(settings.MusicVolume);
      _sfx.SetValueWithoutNotify(settings.SfxVolume);
      _musicValue.text = settings.MusicVolume.ToString();
      _sfxValue.text = settings.SfxVolume.ToString();
    }

    private void Close()
    {
      _finish.SetEnabled(false);
      CloseAsync(LifetimeToken).Forget(exception =>
      {
        if (exception is not OperationCanceledException)
          Debug.LogError($"[SettingsPage] Close failed: {exception}");
      });
    }

    private async UniTask CloseAsync(CancellationToken cancellationToken)
    {
      try
      {
        var result = await _navigator.PopAsync(cancellationToken);
        if (!result.IsSuccess) Debug.LogError($"[SettingsPage] Close failed: {result.Status}. {result.Error}");
      }
      finally
      {
        if (!cancellationToken.IsCancellationRequested) _finish.SetEnabled(true);
      }
    }

    protected override void ReleaseView()
    {
      _subscriptions.Dispose();
      _resolution?.UnregisterValueChangedCallback(OnResolutionChanged);
      _fullScreen?.UnregisterValueChangedCallback(OnWindowModeChanged);
      _music?.UnregisterValueChangedCallback(OnMusicVolumeChanged);
      _sfx?.UnregisterValueChangedCallback(OnSfxVolumeChanged);
      if (_finish != null) _finish.clicked -= Close;
    }
  }
}