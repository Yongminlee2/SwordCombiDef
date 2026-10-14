using System;
using System.Collections.Generic;
using System.Linq;

namespace Swordstellation.Core
{
    public sealed class RunInventory
    {
        public const int SlotCount = 12;
        public const int MaximumSlotCount = 15;

        private readonly SwordInstance[] _slots;

        public event Action Changed;

        public IReadOnlyList<SwordInstance> Slots => _slots;
        public int Capacity => _slots.Length;

        public int EmptyCount => _slots.Count(item => item == null);

        public RunInventory(int slotCount = SlotCount)
        {
            if (slotCount < SlotCount ||
                slotCount > MaximumSlotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotCount));
            }

            _slots = new SwordInstance[slotCount];
        }

        public SwordInstance Get(int slotIndex)
        {
            ValidateSlot(slotIndex);
            return _slots[slotIndex];
        }

        public bool TryAdd(SwordInstance sword, out int slotIndex)
        {
            if (sword == null)
            {
                throw new ArgumentNullException(nameof(sword));
            }

            slotIndex = Array.FindIndex(_slots, item => item == null);
            if (slotIndex < 0)
            {
                return false;
            }

            _slots[slotIndex] = sword;
            Changed?.Invoke();
            return true;
        }

        public void ReplaceSelection(IReadOnlyList<int> selectedIndices, SwordInstance result)
        {
            if (selectedIndices == null || selectedIndices.Count == 0)
            {
                throw new ArgumentException("A forge selection is required.", nameof(selectedIndices));
            }
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            if (selectedIndices.Distinct().Count() != selectedIndices.Count)
            {
                throw new ArgumentException(
                    "Forge selection cannot contain duplicate slots.",
                    nameof(selectedIndices));
            }

            int destination = selectedIndices.Min();
            foreach (int index in selectedIndices)
            {
                ValidateSlot(index);
                if (_slots[index] == null)
                {
                    throw new InvalidOperationException(
                        "Forge selection cannot contain an empty slot.");
                }
            }

            foreach (int index in selectedIndices)
            {
                _slots[index] = null;
            }

            _slots[destination] = result;
            Changed?.Invoke();
        }

        public IReadOnlyList<SwordInstance> GetSelection(IReadOnlyList<int> selectedIndices)
        {
            if (selectedIndices == null)
            {
                throw new ArgumentNullException(nameof(selectedIndices));
            }
            if (selectedIndices.Distinct().Count() != selectedIndices.Count)
            {
                throw new ArgumentException(
                    "Selection cannot contain duplicate slots.",
                    nameof(selectedIndices));
            }

            List<SwordInstance> result = new List<SwordInstance>();
            foreach (int index in selectedIndices)
            {
                ValidateSlot(index);
                if (_slots[index] != null)
                {
                    result.Add(_slots[index]);
                }
            }

            return result;
        }

        // Drop on an empty slot moves the sword; on another sword they trade places.
        public bool TryMoveOrSwap(int sourceIndex, int destinationIndex)
        {
            ValidateSlot(sourceIndex);
            ValidateSlot(destinationIndex);
            if (sourceIndex == destinationIndex ||
                _slots[sourceIndex] == null)
            {
                return false;
            }

            SwordInstance moved = _slots[sourceIndex];
            _slots[sourceIndex] = _slots[destinationIndex];
            _slots[destinationIndex] = moved;
            Changed?.Invoke();
            return true;
        }

        public bool TryRemove(int slotIndex, out SwordInstance removed)
        {
            ValidateSlot(slotIndex);
            removed = _slots[slotIndex];
            if (removed == null)
            {
                return false;
            }

            _slots[slotIndex] = null;
            Changed?.Invoke();
            return true;
        }

        public bool TryMergeSame(
            int sourceIndex,
            int destinationIndex,
            out SwordInstance result)
        {
            ValidateSlot(sourceIndex);
            ValidateSlot(destinationIndex);
            result = null;

            if (sourceIndex == destinationIndex)
            {
                return false;
            }

            SwordInstance source = _slots[sourceIndex];
            SwordInstance destination = _slots[destinationIndex];
            if (source == null ||
                destination == null ||
                source.DefinitionId != destination.DefinitionId ||
                source.Stars != destination.Stars ||
                destination.Stars >= RunBalanceRules.MaximumStars)
            {
                return false;
            }

            result = new SwordInstance(
                destination.DefinitionId,
                destination.Stars + 1,
                source.AppearanceSeed ^
                destination.AppearanceSeed ^
                0xC057E11A710FUL,
                Guid.NewGuid().ToString("N"));
            _slots[sourceIndex] = null;
            _slots[destinationIndex] = result;
            Changed?.Invoke();
            return true;
        }

        private void ValidateSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }
        }
    }
}
