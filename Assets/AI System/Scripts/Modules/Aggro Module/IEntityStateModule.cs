using System;
using Archura.Aggro;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    public interface IEntityStateModule
    {
        GameEntity CurrentTarget { get; }
        EngagementState CurrentState { get; }
        event Action<TargetChangeEventArgs> OnTargetChanged;
        event Action<EngagementChangeEventArgs> OnEngagementChanged;
    }
}
