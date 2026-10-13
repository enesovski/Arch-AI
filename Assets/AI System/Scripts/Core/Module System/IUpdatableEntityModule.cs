using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public interface IUpdatableEntityModule : IEntityModule
    {
        void PerformUpdate(float deltaTime);
    }
}
