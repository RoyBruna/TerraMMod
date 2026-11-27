namespace TerrariaModMenu.Core.Core
{
    internal sealed class ModConfig
    {
        public static ModConfig Instance { get; } = new ModConfig();

        private readonly object _timeLock = new object();
        private double _frozenTimeValue = 27000.0;

        public volatile bool GodMode;
        public volatile bool InfiniteMana;
        public volatile bool NoFallDamage;
        public volatile bool InfiniteFlight;
        public volatile bool InfiniteBreathAndLava;
        public volatile bool NoKnockback;
        public volatile bool NoClip;
        public volatile float NoClipSpeed = 14f;

        public volatile float MoveSpeedMultiplier = 1.0f;
        public volatile float JumpBoostBonus = 0.0f;
        public volatile int BonusDefense = 0;
        public volatile int BonusLifeRegen = 0;
        public volatile int ReachBonusTiles = 0;
        public volatile float MiningSpeedMultiplier = 1.0f;
        public volatile float GlobalDamageMultiplier = 1.0f;
        public volatile int BonusCritChance = 0;

        public volatile bool RapidFireMelee;
        public volatile int RapidFireMeleeTicks = 4;
        public volatile bool RapidFireRanged;
        public volatile int RapidFireRangedTicks = 3;
        public volatile bool RapidFireMagic;
        public volatile int RapidFireMagicTicks = 3;
        public volatile bool RapidFireThrowables;
        public volatile int RapidFireThrowablesTicks = 4;
        public volatile bool RapidFireTools;
        public volatile int RapidFireToolsTicks = 2;
        public volatile bool RapidFireBlocks;
        public volatile int RapidFireBlocksTicks = 2;

        public volatile bool ForceAutoReuseAll;
        public volatile bool InfiniteAmmo;
        public volatile bool InfiniteThrowables;
        public volatile bool InfiniteBlocks;

        public volatile bool ProjectileMultishotEnabled;
        public volatile int ProjectileMultishotCount = 3;
        public volatile float ProjectileMultishotSpreadDeg = 24f;
        public volatile bool ProjectileUniversalHoming;
        public volatile float ProjectileHomingRange = 1100f;
        public volatile float ProjectileHomingTurn = 0.22f;
        public volatile bool ProjectileNoTileCollide;
        public volatile bool ProjectileInfinitePierce;
        public volatile float ProjectileSpeedMultiplier = 1.0f;

        public volatile bool CraftIgnoreStations;
        public volatile bool CraftFreeNoConsume;
        public volatile bool CraftUnlockAllRecipes;

        public volatile bool InfiniteBuffDuration;
        public volatile bool AutoPurgeDebuffs;
        public volatile bool NoPotionCooldown;
        public volatile bool InfinitePotions;
        public volatile bool SuperHealAndManaPotions;

        public volatile bool OpPotionsEnabled;
        public volatile float OpPotionMultiplier = 5.0f;
        public volatile bool OpIronskin = true;
        public volatile bool OpEndurance = true;
        public volatile bool OpSwiftness = true;
        public volatile bool OpDamagePotions = true;
        public volatile bool OpRegeneration = true;
        public volatile bool OpLifeforce = true;
        public volatile bool OpMiningBuilder = true;

        public volatile bool Fullbright;
        public volatile bool VisionSpelunker;
        public volatile bool VisionHunter;
        public volatile bool VisionDangerSense;
        public volatile bool VisionNightVision;

        public volatile bool FreezeTime;
        public double FrozenTimeValue
        {
            get { lock (_timeLock) return _frozenTimeValue; }
            set { lock (_timeLock) _frozenTimeValue = value; }
        }
        public volatile bool FrozenDayTime = true;
        public volatile int CustomDayRate = 1;

        public volatile int SpawnRateMultiplier = 1;
        public volatile bool HotkeyTeleportCursor;

        public volatile bool FishingMultiBobberEnabled;
        public volatile int FishingBobberCount = 5;
        public volatile int FishingCastSpreadPercent = 35;
        public volatile bool FishingInstantBite;
        public volatile bool FishingAutoReel;
        public volatile bool FishingAutoRecast;
        public volatile int FishingAutoReelFilter = 0;
        public volatile bool FishingInfiniteBait;
        public volatile bool FishingVirtualBait;
        public volatile bool FishingNeverBreakLine;
        public volatile bool FishingLavaEnabled;
        public volatile bool FishingSonarAndCrate;
        public volatile bool FishingIgnorePondSize;
        public volatile int FishingPowerBonus = 0;
        public volatile int FishingCrateChance = 0;
        public volatile bool FishingGuaranteedCrate;
        public volatile bool FishingBoostRare;
        public volatile bool FishingAllowMultipleQuestFish;

        public volatile int BonusMaxMinions = 0;
        public volatile int BonusMaxSentries = 0;
        public volatile float SummonDamageMultiplier = 1.0f;
        public volatile float BonusMinionKnockback = 0.0f;
        public volatile float WhipRangeMultiplier = 1.0f;
        public volatile float WhipSpeedMultiplier = 1.0f;
        public volatile int BonusMaxWhipTags = 0;
        public volatile float BonusWhipTagDuration = 0.0f;

        public volatile bool ChaosFreezeEnemies;
        public volatile bool ChaosEnemyMagnetToCursor;
        public volatile bool ChaosItemVacuum;
        public volatile bool HotkeySpawnNpcAtCursor;
        public volatile int SelectedSpawnNpcId = 4;
        public volatile int SelectedSpawnNpcCount = 1;
        public volatile int SelectedSpawnLocationMode = 0;

        public bool TryGetRapidFireTicks(ItemCategory category, out int maxTicks)
        {
            switch (category)
            {
                case ItemCategory.Melee:
                    maxTicks = RapidFireMeleeTicks;
                    return RapidFireMelee;
                case ItemCategory.Ranged:
                    maxTicks = RapidFireRangedTicks;
                    return RapidFireRanged;
                case ItemCategory.MagicOrSummon:
                    maxTicks = RapidFireMagicTicks;
                    return RapidFireMagic;
                case ItemCategory.Throwable:
                    maxTicks = RapidFireThrowablesTicks;
                    return RapidFireThrowables;
                case ItemCategory.Tool:
                    maxTicks = RapidFireToolsTicks;
                    return RapidFireTools;
                case ItemCategory.BlockOrWall:
                    maxTicks = RapidFireBlocksTicks;
                    return RapidFireBlocks;
                default:
                    maxTicks = 0;
                    return false;
            }
        }

        public void ResetFishingOnly()
        {
            FishingMultiBobberEnabled = false;
            FishingBobberCount = 5;
            FishingCastSpreadPercent = 35;
            FishingInstantBite = false;
            FishingAutoReel = false;
            FishingAutoRecast = false;
            FishingAutoReelFilter = 0;
            FishingInfiniteBait = false;
            FishingVirtualBait = false;
            FishingNeverBreakLine = false;
            FishingLavaEnabled = false;
            FishingSonarAndCrate = false;
            FishingIgnorePondSize = false;
            FishingPowerBonus = 0;
            FishingCrateChance = 0;
            FishingGuaranteedCrate = false;
            FishingBoostRare = false;
            FishingAllowMultipleQuestFish = false;
        }

        public void ResetSummonerOnly()
        {
            BonusMaxMinions = 0;
            BonusMaxSentries = 0;
            SummonDamageMultiplier = 1.0f;
            BonusMinionKnockback = 0.0f;
            WhipRangeMultiplier = 1.0f;
            WhipSpeedMultiplier = 1.0f;
            BonusMaxWhipTags = 0;
            BonusWhipTagDuration = 0.0f;
        }

        public void ResetChaosOnly()
        {
            SpawnRateMultiplier = 1;
            ChaosFreezeEnemies = false;
            ChaosEnemyMagnetToCursor = false;
            ChaosItemVacuum = false;
            HotkeySpawnNpcAtCursor = false;
        }

        public void ResetAll()
        {
            GodMode = false;
            InfiniteMana = false;
            NoFallDamage = false;
            InfiniteFlight = false;
            InfiniteBreathAndLava = false;
            NoKnockback = false;
            NoClip = false;
            NoClipSpeed = 14f;

            MoveSpeedMultiplier = 1.0f;
            JumpBoostBonus = 0.0f;
            BonusDefense = 0;
            BonusLifeRegen = 0;
            ReachBonusTiles = 0;
            MiningSpeedMultiplier = 1.0f;
            GlobalDamageMultiplier = 1.0f;
            BonusCritChance = 0;

            RapidFireMelee = false;
            RapidFireRanged = false;
            RapidFireMagic = false;
            RapidFireThrowables = false;
            RapidFireTools = false;
            RapidFireBlocks = false;
            ForceAutoReuseAll = false;
            InfiniteAmmo = false;
            InfiniteThrowables = false;
            InfiniteBlocks = false;

            ProjectileMultishotEnabled = false;
            ProjectileMultishotCount = 3;
            ProjectileMultishotSpreadDeg = 24f;
            ProjectileUniversalHoming = false;
            ProjectileHomingRange = 1100f;
            ProjectileHomingTurn = 0.22f;
            ProjectileNoTileCollide = false;
            ProjectileInfinitePierce = false;
            ProjectileSpeedMultiplier = 1.0f;

            CraftIgnoreStations = false;
            CraftFreeNoConsume = false;
            CraftUnlockAllRecipes = false;

            InfiniteBuffDuration = false;
            AutoPurgeDebuffs = false;
            NoPotionCooldown = false;
            InfinitePotions = false;
            SuperHealAndManaPotions = false;
            OpPotionsEnabled = false;
            OpPotionMultiplier = 5.0f;
            OpIronskin = true;
            OpEndurance = true;
            OpSwiftness = true;
            OpDamagePotions = true;
            OpRegeneration = true;
            OpLifeforce = true;
            OpMiningBuilder = true;

            Fullbright = false;
            VisionSpelunker = false;
            VisionHunter = false;
            VisionDangerSense = false;
            VisionNightVision = false;
            FreezeTime = false;
            CustomDayRate = 1;
            SpawnRateMultiplier = 1;
            HotkeyTeleportCursor = false;

            ResetFishingOnly();
            ResetSummonerOnly();
            ResetChaosOnly();
        }
    }
}
