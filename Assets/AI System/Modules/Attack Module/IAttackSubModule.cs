using System;

namespace Artifika.AI.Attack
{
    public interface IAttackSubModule 
    {
        void Attack(BaseAttackDefinition def);

        event Action OnAttack;

    }
}

