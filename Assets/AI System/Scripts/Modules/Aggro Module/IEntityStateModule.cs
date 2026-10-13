using System;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public interface IEntityStateModule
    {
            GameEntity CurrentTarget { get; }
            event Action<TargetChangeEventArgs> OnTargetChanged;
    }
}
