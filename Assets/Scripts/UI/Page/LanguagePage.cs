using System;
using System.Threading;
using Architecture;
using Architecture.Data.Settings;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
  public sealed class LanguagePage : BasePage
  {
    private readonly ILanguageService _language;
    private readonly IPageNavigator _navigator;
    private readonly ISettingsService _settings;
    private Button _chinese;
    private VisualElement _choices;
    private Button _english;
    private Button _japanese;

    private Label _prompt;

    public LanguagePage(ILanguageService language, ISettingsService settings, IPageNavigator navigator)
    {
      _language = language;
      _settings = settings;
      _navigator = navigator;
    }

    protected override void BindView()
    {
      _prompt = Element<Label>("languagePrompt");
      _choices = Element<VisualElement>("languageChoices");
      _chinese = Element<Button>("chinese");
      _english = Element<Button>("english");
      _japanese = Element<Button>("japanese");
      _chinese.clicked += ChooseChinese;
      _english.clicked += ChooseEnglish;
      _japanese.clicked += ChooseJapanese;
      UpdatePrompt(_language.CurrentLanguage);
    }

    public override UniTask OnPause(CancellationToken cancellationToken)
    {
      return FadeOutAsync(cancellationToken);
    }

    private void ChooseChinese()
    {
      Choose(GameLanguageType.Chinese);
    }

    private void ChooseEnglish()
    {
      Choose(GameLanguageType.English);
    }

    private void ChooseJapanese()
    {
      Choose(GameLanguageType.Japanese);
    }

    private void Choose(GameLanguageType language)
    {
      _choices.SetEnabled(false);
      ApplyLanguageAsync(language, LifetimeToken).Forget(exception =>
      {
        if (exception is not OperationCanceledException)
          Debug.LogError($"[LanguagePage] Could not apply {language}: {exception}");
      });
    }

    private async UniTask ApplyLanguageAsync(GameLanguageType language, CancellationToken cancellationToken)
    {
      var previous = _language.CurrentLanguage;
      try
      {
        var change = await _language.SetLanguageAsync(language, cancellationToken);
        if (!change.IsSuccess)
        {
          Debug.LogError($"[LanguagePage] Locale change failed: {change.Status}. {change.Error}");
          return;
        }

        var save = _settings.SetLanguage(language);
        if (!save.IsSuccess)
        {
          var rollback = await _language.SetLanguageAsync(previous, cancellationToken);
          Debug.LogError(
            $"[LanguagePage] Language setting save failed: {save.Error}. Locale rollback: {rollback.Status}.");
          return;
        }

        UpdatePrompt(language);
        var navigation = await _navigator.ReplaceAsync<MainScenePage>(cancellationToken);
        if (!navigation.IsSuccess)
          Debug.LogError($"[LanguagePage] Navigation failed: {navigation.Status}. {navigation.Error}");
      }
      finally
      {
        if (!cancellationToken.IsCancellationRequested) _choices.SetEnabled(true);
      }
    }

    private void UpdatePrompt(GameLanguageType language)
    {
      UIRoot.ApplyLanguage(_prompt, language);
      _prompt.text = language switch
      {
        GameLanguageType.Chinese => "这是正确的语言吗？",
        GameLanguageType.Japanese => "これは正しい言語ですか？",
        _ => "Is this the correct language?"
      };
    }

    protected override void ReleaseView()
    {
      if (_chinese != null) _chinese.clicked -= ChooseChinese;
      if (_english != null) _english.clicked -= ChooseEnglish;
      if (_japanese != null) _japanese.clicked -= ChooseJapanese;
    }
  }
}