using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace UI
{
  public interface IBasePage
  {
    UniTask OnEnter(CancellationToken cancellationToken);
    UniTask OnPause(CancellationToken cancellationToken);
    UniTask OnResume(CancellationToken cancellationToken);
    UniTask OnExit(CancellationToken cancellationToken);
  }

  public abstract class BasePage : IBasePage, IDisposable
  {
    protected const string HiddenClass = "is-hidden";
    protected const string TransparentClass = "is-transparent";

    private const int PageFadeMilliseconds = 500;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _disposed;

    public VisualElement View { get; private set; }
    protected CancellationToken LifetimeToken => _lifetime.Token;

    public virtual async UniTask OnEnter(CancellationToken cancellationToken)
    {
      View.AddToClassList("page-fade");
      View.AddToClassList(TransparentClass);
      await RevealAsync(View, PageFadeMilliseconds, cancellationToken);
      View.SetEnabled(true);
    }

    public virtual UniTask OnPause(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      View.SetEnabled(false);
      View.AddToClassList(HiddenClass);
      return UniTask.CompletedTask;
    }

    public virtual UniTask OnResume(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      View.RemoveFromClassList(TransparentClass);
      View.RemoveFromClassList(HiddenClass);
      View.SetEnabled(true);
      return UniTask.CompletedTask;
    }

    public virtual UniTask OnExit(CancellationToken cancellationToken)
    {
      return FadeOutAsync(cancellationToken);
    }

    public void Dispose()
    {
      if (_disposed) return;

      _disposed = true;
      _lifetime.Cancel();
      ReleaseView();
      View?.RemoveFromHierarchy();
      _lifetime.Dispose();
    }

    public void Initialize(VisualElement view)
    {
      View = view;
      View.SetEnabled(false);
      BindView();
    }

    protected abstract void BindView();

    protected T Element<T>(string name) where T : VisualElement
    {
      return View.Q<T>(name) ?? throw new InvalidOperationException($"Missing {typeof(T).Name} '{name}'.");
    }

    protected async UniTask FadeOutAsync(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      View.SetEnabled(false);
      if (View.ClassListContains(HiddenClass)) return;

      View.AddToClassList(TransparentClass);
      await WaitForFadeAsync(PageFadeMilliseconds, cancellationToken);
      View.AddToClassList(HiddenClass);
    }

    protected static async UniTask RevealAsync(
      VisualElement element, int fadeMilliseconds, CancellationToken cancellationToken)
    {
      element.RemoveFromClassList(HiddenClass);
      await UniTask.NextFrame(cancellationToken);
      element.RemoveFromClassList(TransparentClass);
      await WaitForFadeAsync(fadeMilliseconds, cancellationToken);
    }

    protected static UniTask WaitForFadeAsync(int milliseconds, CancellationToken cancellationToken)
    {
      return UniTask.Delay(milliseconds, true, cancellationToken: cancellationToken);
    }

    protected virtual void ReleaseView()
    {
    }
  }
}