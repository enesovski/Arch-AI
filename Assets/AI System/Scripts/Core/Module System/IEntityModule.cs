using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    public interface IEntityModule
    {
        GameObject GameObject { get; }

        Transform Transform { get; }

        void Initialize();

        void OnModuleEnabled();

        void OnModuleDisabled();

        bool IsEnabled { get; }
    }
}
