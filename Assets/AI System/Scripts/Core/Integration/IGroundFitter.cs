using UnityEngine;

namespace Archura
{
    /// <summary>
    /// Optional ground-alignment component (slope fitting, root-motion handling).
    /// When present, <see cref="RotationModule"/> drives its yaw so the two never fight.
    /// </summary>
    public interface IGroundFitter
    {
        /// <summary>When true, the fitter takes its yaw from <see cref="ExternalYaw"/>.</summary>
        bool UseExternalYaw { get; set; }
        float ExternalYaw { get; set; }

        /// <summary>Extra yaw the fitter applies on top of its own; the AI resets it to zero.</summary>
        float UpAxisRotation { get; set; }

        /// <summary>Suppresses root-motion translation while combat movement steers the agent.</summary>
        bool SuppressRootMotionTranslation { get; set; }

        bool HandleRootMotion { get; set; }
        LayerMask GroundLayerMask { get; }

        void Initialize(Blackboard blackboard);
    }
}
