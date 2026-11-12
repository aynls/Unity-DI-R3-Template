using System;
using Architecture;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace UI
{
  [RequireComponent(typeof(UIDocument))]
  public sealed class UIRoot : MonoBehaviour
  {
    [SerializeField] private VisualTreeAsset languagePage;
    [SerializeField] private VisualTreeAsset mainScenePage;
    [SerializeField] private VisualTreeAsset settingsPage;

    [Inject] private ILanguageService _language;

    public VisualTreeAsset GetTemplate(Type pageType)
    {
      if (pageType == typeof(LanguagePage)) return languagePage;
      if (pageType == typeof(MainScenePage)) return mainScenePage;
      if (pageType == typeof(SettingsPage)) return settingsPage;
      return null;
    }

    public void Mount(VisualElement view)
    {
      var root = GetComponent<UIDocument>().rootVisualElement;
      view.AddToClassList("page-view");
      ApplyLanguage(view, _language.CurrentLanguage);
      root.Add(view);
    }

    public static void ApplyLanguage(VisualElement element, GameLanguageType language)
    {
      element.EnableInClassList("font-chinese", language == GameLanguageType.Chinese);
      element.EnableInClassList("font-japanese", language == GameLanguageType.Japanese);
      element.EnableInClassList("font-english", language == GameLanguageType.English);
    }
  }
}