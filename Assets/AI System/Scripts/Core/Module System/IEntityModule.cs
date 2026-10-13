using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
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
