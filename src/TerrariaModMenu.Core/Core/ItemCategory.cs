using Terraria;

namespace TerrariaModMenu.Core.Core
{
    public enum ItemCategory
    {
        Other = 0,
        Melee,
        Ranged,
        MagicOrSummon,
        Throwable,
        Tool,
        BlockOrWall,
        Potion
    }

    internal static class ItemClassifier
    {
        public static ItemCategory Classify(Item? item)
        {
            if (item == null || item.IsAir || item.type <= 0)
            {
                return ItemCategory.Other;
            }

            if (item.potion || item.healLife > 0 || item.healMana > 0)
            {
                return ItemCategory.Potion;
            }

            if (item.pick > 0 || item.axe > 0 || item.hammer > 0)
            {
                return ItemCategory.Tool;
            }

            if (item.createTile >= 0 || item.createWall >= 0)
            {
                return ItemCategory.BlockOrWall;
            }

            if (item.consumable && (item.damage > 0 || item.shoot > 0))
            {
                return ItemCategory.Throwable;
            }

            if (item.magic || item.summon)
            {
                return ItemCategory.MagicOrSummon;
            }

            if (item.ranged)
            {
                return ItemCategory.Ranged;
            }

            if (item.melee)
            {
                return ItemCategory.Melee;
            }

            return ItemCategory.Other;
        }
    }
}
