using System;
using Archura.Attack;
using UnityEngine;

namespace Archura
{
    /// <summary>
    /// Bridges the attack keyframe of an animation to <c>AttackModule</c>, which
    /// deals damage only when this event fires.
    /// </summary>
    public interface IAttackAnimationEventSource
    {
        event Action<BaseAttackDefinition> AttackAnimationTriggered;
        void TriggerAttackAnimation(BaseAttackDefinition attackDefinition);
    }

    /// <summary>Plays a keyed presentation effect requested by an animation event.</summary>
    public interface IAnimationEffectPlayer
    {
        void PlayEffect(string key);
    }

    /// <summary>Short on-hit flash/outline (e.g. a highlight or rim-light effect).</summary>
    public interface IHitHighlight
    {
        void PlayHitHighlight(Vector3 impactPoint);
    }

    /// <summary>Receives named animation events the AI does not handle itself.</summary>
    public interface IAnimationEventReceiver
    {
        void OnAnimationEvent(string eventName);
    }
}
