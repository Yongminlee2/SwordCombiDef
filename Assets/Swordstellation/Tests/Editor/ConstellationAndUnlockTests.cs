using System.Linq;
using NUnit.Framework;
using Swordstellation.Core;

namespace Swordstellation.Tests
{
    public sealed class ConstellationAndUnlockTests
    {
        // Board columns are 3 wide: slot 0 | 1 | 2 on the top row, 3 | 4 | 5 below.
        [Test]
        public void Group_CountsOrthogonalNeighboursOfTheSameElement()
        {
            ContentCatalog catalog = new ContentCatalog();
            RunInventory board = Board(catalog,
                (0, "ember_blade"), (1, "ember_blade"), (3, "ember_blade"),
                (4, "frost_blade"), (5, "ember_blade"));

            Assert.That(ConstellationRules.GroupSize(board, catalog, 0), Is.EqualTo(3));
            Assert.That(ConstellationRules.GroupSize(board, catalog, 3), Is.EqualTo(3));
            Assert.That(ConstellationRules.GroupSize(board, catalog, 5), Is.EqualTo(1));
            Assert.That(ConstellationRules.GroupSize(board, catalog, 4), Is.EqualTo(1));
            Assert.That(ConstellationRules.GroupSize(board, catalog, 7), Is.EqualTo(0));
        }

        [Test]
        public void Group_DoesNotWrapAcrossRows()
        {
            ContentCatalog catalog = new ContentCatalog();
            RunInventory board = Board(catalog, (2, "ember_blade"), (3, "ember_blade"));

            Assert.That(ConstellationRules.GroupSize(board, catalog, 2), Is.EqualTo(1));
        }

        [Test]
        public void Group_IgnoresNeutralSwords()
        {
            ContentCatalog catalog = new ContentCatalog();
            RunInventory board = Board(catalog, (0, "resonance_blade"), (1, "iron_shard_blade"));

            Assert.That(ConstellationRules.GroupSize(board, catalog, 0), Is.EqualTo(0));
        }

        [Test]
        public void GroupBonus_GrowsWithSizeAndCaps()
        {
            Assert.That(ConstellationRules.GroupBonus(1), Is.EqualTo(0f));
            Assert.That(ConstellationRules.GroupBonus(2), Is.EqualTo(0.10f).Within(0.0001f));
            Assert.That(ConstellationRules.GroupBonus(3), Is.EqualTo(0.20f).Within(0.0001f));
            Assert.That(ConstellationRules.GroupBonus(4), Is.EqualTo(0.30f).Within(0.0001f));
            Assert.That(ConstellationRules.GroupBonus(9), Is.EqualTo(0.40f).Within(0.0001f));
        }

        [Test]
        public void Links_ListEachAdjacentSameElementPairOnce()
        {
            ContentCatalog catalog = new ContentCatalog();
            RunInventory board = Board(catalog,
                (0, "ember_blade"), (1, "ember_blade"), (3, "ember_blade"), (2, "frost_blade"));

            var links = ConstellationRules.Links(board, catalog);

            Assert.That(links.Count, Is.EqualTo(2));
            Assert.That(links.Any(link => link.First == 0 && link.Second == 1), Is.True);
            Assert.That(links.Any(link => link.First == 0 && link.Second == 3), Is.True);
        }

        [Test]
        public void MoveOrSwap_MovesIntoEmptyAndSwapsOccupied()
        {
            ContentCatalog catalog = new ContentCatalog();
            RunInventory board = Board(catalog, (0, "ember_blade"), (1, "frost_blade"));

            Assert.That(board.TryMoveOrSwap(0, 5), Is.True);
            Assert.That(board.Get(0), Is.Null);
            Assert.That(board.Get(5).DefinitionId, Is.EqualTo("ember_blade"));

            Assert.That(board.TryMoveOrSwap(5, 1), Is.True);
            Assert.That(board.Get(1).DefinitionId, Is.EqualTo("ember_blade"));
            Assert.That(board.Get(5).DefinitionId, Is.EqualTo("frost_blade"));

            Assert.That(board.TryMoveOrSwap(2, 3), Is.False);
            Assert.That(board.TryMoveOrSwap(1, 1), Is.False);
        }

        [Test]
        public void Constellation_RaisesBoardPower_AndMasteryDoublesIt()
        {
            ContentCatalog catalog = new ContentCatalog();
            CombinationDefenseSession apart = Session(catalog, Board(catalog, (0, "ember_blade"), (8, "ember_blade")));
            CombinationDefenseSession together = Session(catalog, Board(catalog, (0, "ember_blade"), (1, "ember_blade")));
            CombinationDefenseSession mastered = Session(catalog, Board(catalog, (0, "ember_blade"), (1, "ember_blade")));
            mastered.ApplyRune(RunContentCatalog.GetRune("constellation_mastery"));

            Assert.That(together.BoardPower(), Is.EqualTo(apart.BoardPower() * 1.10f).Within(0.01f));
            Assert.That(mastered.BoardPower(), Is.EqualTo(apart.BoardPower() * 1.20f).Within(0.01f));
        }

        [Test]
        public void Unlocks_StartWithSixEngravingsAndOpenOnePerLevel()
        {
            var engravings = RunContentCatalog.Runes.Where(rune => rune.Tag != RuneTag.Basic).ToList();

            Assert.That(engravings.Count(rune => rune.UnlockLevel <= 1), Is.EqualTo(6));
            Assert.That(
                RunContentCatalog.Runes.Where(rune => rune.Tag == RuneTag.Basic).All(rune => rune.UnlockLevel <= 1),
                Is.True);
            for (int level = 2; level <= 14; level++)
            {
                Assert.That(engravings.Count(rune => rune.UnlockLevel == level), Is.EqualTo(1), $"LV.{level}");
            }
            Assert.That(RunContentCatalog.NextUnlock(1).UnlockLevel, Is.EqualTo(2));
            Assert.That(RunContentCatalog.NextUnlock(14), Is.Null);
        }

        [Test]
        public void Draw_NeverOffersLockedEngravings()
        {
            for (ulong seed = 1; seed <= 80; seed++)
            {
                var drawn = RunContentCatalog.DrawRunes(new SeededRandom(seed), new string[0], seed % 5 == 0, 1);

                Assert.That(drawn.All(rune => rune.UnlockLevel <= 1), Is.True, $"seed {seed}");
                Assert.That(drawn.Count, Is.EqualTo(3));
            }
        }

        private static RunInventory Board(ContentCatalog catalog, params (int slot, string id)[] swords)
        {
            RunInventory board = new RunInventory();
            SwordInstance[] placed = new SwordInstance[board.Capacity];
            foreach ((int slot, string id) in swords)
            {
                placed[slot] = new SwordInstance(id, 1, (ulong)(slot + 1), $"s{slot}");
            }
            // Fill in slot order, then drop the placeholders to leave real gaps.
            for (int index = 0; index < board.Capacity; index++)
            {
                board.TryAdd(placed[index] ?? new SwordInstance("ember_blade", 1, 99, $"gap{index}"), out _);
            }
            for (int index = 0; index < board.Capacity; index++)
            {
                if (placed[index] == null)
                {
                    board.TryRemove(index, out _);
                }
            }
            return board;
        }

        private static CombinationDefenseSession Session(ContentCatalog catalog, RunInventory board)
        {
            return new CombinationDefenseSession(
                catalog,
                catalog.GetStage("stage_starlit_forge"),
                RunMode.Standard,
                board,
                777,
                0,
                150);
        }
    }
}
