using System.Collections;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class BossSpawnAnchor : EntitySpawnAnchor
    {
        [SerializeField] private WorldFactKey bossDefeatedKey;

        public WorldFactKey BossDefeatedKey => bossDefeatedKey;

        protected override IEnumerator WaitForSpawnReadiness()
        {
            while (WorldFactsService.Instance == null || !WorldFactsService.Instance.IsNetworkDataReady)
                yield return null;
        }

        protected override bool ShouldSpawnEntity()
        {
            return bossDefeatedKey == null
                   || WorldFactsService.Instance == null
                   || !WorldFactsService.Instance.GetBool(bossDefeatedKey.Key);
        }
    }
}
