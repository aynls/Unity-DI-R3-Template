using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace UI
{
  public sealed class PageStack
  {
    private readonly List<BasePage> _entries = new();
    private readonly IObjectResolver _resolver;
    private readonly UIRoot _uiRoot;

    public PageStack(IObjectResolver resolver, UIRoot uiRoot)
    {
      _resolver = resolver;
      _uiRoot = uiRoot;
    }

    private BasePage Top => _entries.Count == 0 ? null : _entries[^1];

    public UniTask<NavigationResult> PushAsync<TPage>(CancellationToken cancellationToken)
      where TPage : BasePage
    {
      return OpenAsync<TPage>(false, cancellationToken);
    }

    public UniTask<NavigationResult> ReplaceAsync<TPage>(CancellationToken cancellationToken)
      where TPage : BasePage
    {
      return OpenAsync<TPage>(true, cancellationToken);
    }

    private async UniTask<NavigationResult> OpenAsync<TPage>(
      bool replace,
      CancellationToken cancellationToken) where TPage : BasePage
    {
      var previous = Top;
      if (previous is TPage) return new NavigationResult(NavigationStatus.AlreadyCurrent);

      var template = _uiRoot.GetTemplate(typeof(TPage));
      if (template == null)
        return NavigationResult.Failure(
          NavigationStatus.InvalidPage, $"No UXML template assigned for {typeof(TPage).Name}.");

      BasePage page = null;
      try
      {
        page = _resolver.Resolve<TPage>();
        page.Initialize(template.Instantiate());
      }
      catch (Exception exception)
      {
        page?.Dispose();
        return NavigationResult.Failure(
          NavigationStatus.InvalidPage, $"Cannot create {typeof(TPage).Name}: {exception}");
      }

      try
      {
        if (previous != null) await previous.OnPause(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        _uiRoot.Mount(page.View);
        await page.OnEnter(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        _entries.Add(page);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        page.Dispose();
        throw;
      }
      catch (Exception exception)
      {
        page.Dispose();
        var resumeError = await ResumeAsync(previous, cancellationToken);
        return NavigationResult.Failure(NavigationStatus.LifecycleFailed,
          CombineErrors($"Cannot enter {typeof(TPage).Name}: {exception}", resumeError));
      }

      if (replace && previous != null)
      {
        _entries.Remove(previous);
        var exitError = await ExitAsync(previous, cancellationToken);
        if (exitError != null) Debug.LogError($"[PageStack] Replacement succeeded. {exitError}");
      }

      return NavigationResult.Success();
    }

    public async UniTask<NavigationResult> PopAsync(CancellationToken cancellationToken)
    {
      var page = Top;
      if (page == null) return NavigationResult.Failure(NavigationStatus.Empty, "Page stack is empty.");

      _entries.RemoveAt(_entries.Count - 1);
      var exitError = await ExitAsync(page, cancellationToken);
      var resumeError = await ResumeAsync(Top, cancellationToken);
      return ToResult(CombineErrors(exitError, resumeError));
    }

    public async UniTask<NavigationResult> ClearAsync(CancellationToken cancellationToken)
    {
      string error = null;
      while (Top is { } page)
      {
        _entries.RemoveAt(_entries.Count - 1);
        error = CombineErrors(error, await ExitAsync(page, cancellationToken));
      }

      return ToResult(error);
    }

    public void ClearImmediately()
    {
      for (var index = _entries.Count - 1; index >= 0; index--) _entries[index].Dispose();

      _entries.Clear();
    }

    private static async UniTask<string> ResumeAsync(BasePage page, CancellationToken cancellationToken)
    {
      if (page == null) return null;

      try
      {
        await page.OnResume(cancellationToken);
        return null;
      }
      catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
      {
        return $"Cannot resume {page.GetType().Name}: {exception}";
      }
    }

    private static async UniTask<string> ExitAsync(BasePage page, CancellationToken cancellationToken)
    {
      try
      {
        await page.OnExit(cancellationToken);
        return null;
      }
      catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
      {
        return $"Cannot exit {page.GetType().Name}: {exception}";
      }
      finally
      {
        page.Dispose();
      }
    }

    private static NavigationResult ToResult(string error)
    {
      return error == null
        ? NavigationResult.Success()
        : NavigationResult.Failure(NavigationStatus.LifecycleFailed, error);
    }

    private static string CombineErrors(string first, string second)
    {
      return first == null ? second : second == null ? first : first + " | " + second;
    }
  }
}