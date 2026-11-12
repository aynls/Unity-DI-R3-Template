using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Architecture.Audio.Internal
{
  public enum AudioCueKind
  {
    Music,
    Sfx
  }

  public readonly struct AudioCueKey : IEquatable<AudioCueKey>
  {
    public AudioCueKind Kind { get; }
    public string Value { get; }

    public AudioCueKey(AudioCueKind kind, string value)
    {
      Kind = kind;
      Value = value;
    }

    public bool Equals(AudioCueKey other)
    {
      return Kind == other.Kind && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
      return obj is AudioCueKey other && Equals(other);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return ((int)Kind * 397) ^ (Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value));
      }
    }

    public override string ToString()
    {
      return $"{Kind}:{Value}";
    }
  }

  public enum AudioClipLoadStatus
  {
    Loaded,
    UnknownCue,
    LoadFailed,
    Cancelled,
    ShuttingDown
  }

  public readonly struct AudioClipLoadResult
  {
    public AudioClipLoadStatus Status { get; }
    public AudioClip Clip { get; }
    public float DefaultGain { get; }

    private AudioClipLoadResult(AudioClipLoadStatus status, AudioClip clip, float defaultGain)
    {
      Status = status;
      Clip = clip;
      DefaultGain = defaultGain;
    }

    public static AudioClipLoadResult Loaded(AudioClip clip, float defaultGain)
    {
      return new AudioClipLoadResult(AudioClipLoadStatus.Loaded, clip, defaultGain);
    }

    public static AudioClipLoadResult FromStatus(AudioClipLoadStatus status)
    {
      return new AudioClipLoadResult(status, null, 0f);
    }
  }

  public interface IAudioClipStore : IDisposable
  {
    UniTask<AudioClipLoadResult> LoadAsync(AudioCueKey cueKey, CancellationToken cancellationToken);
  }
}