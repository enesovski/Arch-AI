using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    public interface IUpdatableEntityModule : IEntityModule
    {
        void PerformUpdate(float deltaTime);
    }
}
