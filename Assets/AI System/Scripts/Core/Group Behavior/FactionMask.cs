[System.Flags]
public enum FactionMask
{
    None = 0,
    Player = 1 << (int)FactionType.Player,
    ElythianBots = 1 << (int)FactionType.ElythianBots,
    TamedBots = 1 << (int)FactionType.TamedBots,
    PlayerStructures = 1 << (int)FactionType.PlayerStructures,
    WildAnimals = 1 << (int)FactionType.WildAnimals,
    All = Player | ElythianBots | TamedBots | PlayerStructures | WildAnimals,
}
