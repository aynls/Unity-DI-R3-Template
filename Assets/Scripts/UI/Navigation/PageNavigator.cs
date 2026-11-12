using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UI
{
  public sealed class PageNavigator : IPageNavigator, IDisposable
  {
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly PageStack _stack;
    private bool _isShuttingDown;

    public PageNavigator(PageStack stack)
    {
      _stack = stack;
    }

    public void Dispose()
    {
      if (_isShuttingDown) return;

      _isShuttingDown = true;
      _shutdown.Cancel();
      _stack.ClearImmediately();
      _shutdown.Dispose();
    }

    public UniTask<NavigationResult> PushAsync<TPage>(CancellationToken cancellationToken = default)
      where TPage : BasePage
    {
      return RunAsync(_stack.PushAsync<TPage>, cancellationToken);
    }

    public UniTask<NavigationResult> ReplaceAsync<TPage>(CancellationToken cancellationToken = default)
      where TPage : BasePage
    {
      return RunAsync(_stack.ReplaceAsync<TPage>, cancellationToken);
    }

    public UniTask<NavigationResult> PopAsync(CancellationToken cancellationToken = default)
    {
      return RunAsync(_stack.PopAsync, cancellationToken);
    }

    public UniTask<NavigationResult> ClearAsync(CancellationToken cancellationToken = default)
    {
      return RunAsync(_stack.ClearAsync, cancellationToken);
    }

    private async UniTask<NavigationResult> RunAsync(
      Func<CancellationToken, UniTask<NavigationResult>> navigate,
      CancellationToken callerToken)
    {
      if (_isShuttingDown) return ShuttingDown();

      var shutdownToken = _shutdown.Token;
      using var waiting = CancellationTokenSource.CreateLinkedTokenSource(callerToken, shutdownToken);
      try
      {
        await _gate.WaitAsync(waiting.Token);
        try
        {
          waiting.Token.ThrowIfCancellationRequested();
          return await navigate(shutdownToken);
        }
        finally
        {
          _gate.Release();
        }
      }
      catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
      {
        return ShuttingDown();
      }
    }

    private static NavigationResult ShuttingDown()
    {
      return NavigationResult.Failure(NavigationStatus.ShuttingDown, "Navigator is shutting down.");
    }
  }
}