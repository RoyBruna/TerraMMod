using System;
using Terraria;

namespace TerrariaModMenu.Core.Core
{
    internal readonly struct HeldItemSnapshot
    {
        public bool Valid { get; }
        public int Type { get; }
        public string Name { get; }
        public ItemCategory Category { get; }
        public int Damage { get; }
        public int Crit { get; }
        public int UseTime { get; }
        public int UseAnimation { get; }
        public float ShootSpeed { get; }
        public float KnockBack { get; }
        public float Scale { get; }
        public bool AutoReuse { get; }
        public int Stack { get; }

        public HeldItemSnapshot(
            bool valid, int type, string name, ItemCategory category,
            int damage, int crit, int useTime, int useAnimation,
            float shootSpeed, float knockBack, float scale, bool autoReuse, int stack)
        {
            Valid = valid;
            Type = type;
            Name = name;
            Category = category;
            Damage = damage;
            Crit = crit;
            UseTime = useTime;
            UseAnimation = useAnimation;
            ShootSpeed = shootSpeed;
            KnockBack = knockBack;
            Scale = scale;
            AutoReuse = autoReuse;
            Stack = stack;
        }
    }

    internal readonly struct GameSnapshot
    {
        public bool InWorld { get; }
        public string PlayerName { get; }
        public string WorldName { get; }
        public int NetMode { get; }
        public int Life { get; }
        public int LifeMax { get; }
        public int Mana { get; }
        public int ManaMax { get; }
        public int Defense { get; }
        public int ActiveBuffCount { get; }
        public bool DayTime { get; }
        public double WorldTime { get; }
        public HeldItemSnapshot HeldItem { get; }

        public GameSnapshot(
            bool inWorld, string playerName, string worldName, int netMode,
            int life, int lifeMax, int mana, int manaMax, int defense,
            int activeBuffCount, bool dayTime, double worldTime, HeldItemSnapshot heldItem)
        {
            InWorld = inWorld;
            PlayerName = playerName;
            WorldName = worldName;
            NetMode = netMode;
            Life = life;
            LifeMax = lifeMax;
            Mana = mana;
            ManaMax = manaMax;
            Defense = defense;
            ActiveBuffCount = activeBuffCount;
            DayTime = dayTime;
            WorldTime = worldTime;
            HeldItem = heldItem;
        }
    }

    internal static class GameState
    {
        public static bool TryGetLocalPlayer(out Player? player)
        {
            player = null;
            try
            {
                if (Main.dedServ || Main.gameMenu)
                {
                    return false;
                }

                Player[]? players = Main.player;
                int idx = Main.myPlayer;
                if (players == null || idx < 0 || idx >= players.Length)
                {
                    return false;
                }

                Player? candidate = players[idx];
                if (candidate == null || !candidate.active)
                {
                    return false;
                }

                player = candidate;
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error("GameState.TryGetLocalPlayer", ex);
                player = null;
                return false;
            }
        }

        public static GameSnapshot CaptureSnapshot()
        {
            try
            {
                if (!TryGetLocalPlayer(out Player? player) || player == null)
                {
                    return new GameSnapshot(
                        false, "-", "-", Main.netMode,
                        0, 0, 0, 0, 0, 0, Main.dayTime, Main.time, default);
                }

                int buffs = 0;
                if (player.buffType != null && player.buffTime != null)
                {
                    int len = Math.Min(player.buffType.Length, player.buffTime.Length);
                    for (int i = 0; i < len; i++)
                    {
                        if (player.buffType[i] > 0 && player.buffTime[i] > 0)
                        {
                            buffs++;
                        }
                    }
                }

                HeldItemSnapshot held = default;
                Item? hi = player.HeldItem;
                if (hi != null && !hi.IsAir && hi.type > 0)
                {
                    held = new HeldItemSnapshot(
                        valid: true,
                        type: hi.type,
                        name: string.IsNullOrEmpty(hi.Name) ? $"Item #{hi.type}" : hi.Name,
                        category: ItemClassifier.Classify(hi),
                        damage: hi.damage,
                        crit: hi.crit,
                        useTime: hi.useTime,
                        useAnimation: hi.useAnimation,
                        shootSpeed: hi.shootSpeed,
                        knockBack: hi.knockBack,
                        scale: hi.scale,
                        autoReuse: hi.autoReuse,
                        stack: hi.stack);
                }

                return new GameSnapshot(
                    inWorld: true,
                    playerName: string.IsNullOrEmpty(player.name) ? "<Unnamed>" : player.name,
                    worldName: string.IsNullOrEmpty(Main.worldName) ? "<Unknown>" : Main.worldName,
                    netMode: Main.netMode,
                    life: player.statLife,
                    lifeMax: player.statLifeMax2,
                    mana: player.statMana,
                    manaMax: player.statManaMax2,
                    defense: player.statDefense,
                    activeBuffCount: buffs,
                    dayTime: Main.dayTime,
                    worldTime: Main.time,
                    heldItem: held);
            }
            catch (Exception ex)
            {
                ModLogger.Error("GameState.CaptureSnapshot", ex);
                return default;
            }
        }
    }
}
