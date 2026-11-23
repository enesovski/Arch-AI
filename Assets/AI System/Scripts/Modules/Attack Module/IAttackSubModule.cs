using System;

namespace Archura.Attack
{
    public interface IAttackSubModule
    {
        void Attack(BaseAttackDefinition def);
        event Action OnAttack;
    }
}
