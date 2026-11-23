using System;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    public interface IEntityStateModule
    {
            GameEntity CurrentTarget { get; }
            event Action<TargetChangeEventArgs> OnTargetChanged;
    }
}
