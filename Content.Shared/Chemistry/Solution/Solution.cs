using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry.Solution
{
    /// <summary>
    ///     A solution of reagents.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public class Solution : IEnumerable<Solution.ReagentQuantity>, ISerializationHooks
    {
        // Most objects on the station hold only 1 or 2 reagents
        [ViewVariables]
        [DataField("reagents")]
        private List<ReagentQuantity> _contents = new(2);

        public IReadOnlyList<ReagentQuantity> Contents => _contents;

        /// <summary>
        ///     The calculated total volume of all reagents in the solution (ex. Total volume of liquid in beaker).
        /// </summary>
        [ViewVariables]
        public ReagentUnit TotalVolume { get; private set; }

        public Color Color => GetColor();

        /// <summary>
        ///     Constructs an empty solution (ex. an empty beaker).
        /// </summary>
        public Solution() { }

        /// <summary>
        ///     Constructs a solution containing 100% of a reagent (ex. A beaker of pure water).
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public Solution(string reagentId, ReagentUnit quantity)
        {
            AddReagent(reagentId, quantity);
        }

        void ISerializationHooks.AfterDeserialization()
        {
            TotalVolume = ReagentUnit.Zero;
            _contents.ForEach(reagent => TotalVolume += reagent.Quantity);
        }

        public bool ContainsReagent(string reagentId)
        {
            return ContainsReagent(reagentId, out _);
        }

        public bool ContainsReagent(string reagentId, out ReagentUnit quantity)
        {
            foreach (var reagent in Contents)
            {
                if (reagent.ReagentId == reagentId)
                {
                    quantity = reagent.Quantity;
                    return true;
                }
            }

            quantity = ReagentUnit.New(0);
            return false;
        }

        public string GetPrimaryReagentId()
        {
            if (Contents.Count == 0)
            {
                return "";
            }

            var majorReagent = Contents.OrderByDescending(reagent => reagent.Quantity).First();
            return majorReagent.ReagentId;
        }

        /// <summary>
        ///     Adds a given quantity of a reagent directly into the solution.
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public void AddReagent(string reagentId, ReagentUnit quantity)
        {
            if (quantity <= 0)
                return;

            for (var i = 0; i < _contents.Count; i++)
            {
                var reagent = _contents[i];
                if (reagent.ReagentId != reagentId)
                    continue;

                _contents[i] = new ReagentQuantity(reagentId, reagent.Quantity + quantity);
                TotalVolume += quantity;
                return;
            }

            _contents.Add(new ReagentQuantity(reagentId, quantity));
            TotalVolume += quantity;
        }

        /// <summary>
        ///     Scales the amount of solution.
        /// </summary>
        /// <param name="scale">The scalar to modify the solution by.</param>
        public void ScaleSolution(float scale)
        {
            if (scale == 1) return;
            var tempContents = new List<ReagentQuantity>(_contents);
            foreach(ReagentQuantity current in tempContents)
            {
                if(scale > 1)
                {
                    AddReagent(current.ReagentId, current.Quantity * scale - current.Quantity);
                }
                else
                {
                    RemoveReagent(current.ReagentId, current.Quantity - current.Quantity * scale);
                }
            }
        }

        /// <summary>
        ///     Returns the amount of a single reagent inside the solution.
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <returns>The quantity in milli-units.</returns>
        public ReagentUnit GetReagentQuantity(string reagentId)
        {
            for (var i = 0; i < _contents.Count; i++)
            {
                if (_contents[i].ReagentId == reagentId)
                    return _contents[i].Quantity;
            }

            return ReagentUnit.New(0);
        }

        public void RemoveReagent(string reagentId, ReagentUnit quantity)
        {
            if(quantity <= 0)
                return;

            for (var i = 0; i < _contents.Count; i++)
            {
                var reagent = _contents[i];
                if(reagent.ReagentId != reagentId)
                    continue;

                var curQuantity = reagent.Quantity;

                var newQuantity = curQuantity - quantity;
                if (newQuantity <= 0)
                {
                    _contents.RemoveSwap(i);
                    TotalVolume -= curQuantity;
                }
                else
                {
                    _contents[i] = new ReagentQuantity(reagentId, newQuantity);
                    TotalVolume -= quantity;
                }

                return;
            }
        }

        /// <summary>
        /// Remove the specified quantity from this solution.
        /// </summary>
        /// <param name="quantity">The quantity of this solution to remove</param>
        public void RemoveSolution(ReagentUnit quantity)
        {
            if(quantity <= 0)
                return;

            var ratio = (TotalVolume - quantity).Double() / TotalVolume.Double();

            if (ratio <= 0)
            {
                RemoveAllSolution();
                return;
            }

            for (var i = 0; i < _contents.Count; i++)
            {
                var reagent = _contents[i];
                var oldQuantity = reagent.Quantity;

                // quantity taken is always a little greedy, so fractional quantities get rounded up to the nearest
                // whole unit. This should prevent little bits of chemical remaining because of float rounding errors.
                var newQuantity = oldQuantity * ratio;

                _contents[i] = new ReagentQuantity(reagent.ReagentId, newQuantity);
            }

            TotalVolume = TotalVolume * ratio;
        }

        public void RemoveAllSolution()
        {
            _contents.Clear();
            TotalVolume = ReagentUnit.New(0);
        }

        public Solution SplitSolution(ReagentUnit quantity)
        {
            if (quantity <= 0)
                return new Solution();

            Solution newSolution;

            if (quantity >= TotalVolume)
            {
                newSolution = Clone();
                RemoveAllSolution();
                return newSolution;
            }

            newSolution = new Solution();
            var newTotalVolume = ReagentUnit.New(0);
            var remainingVolume = TotalVolume;

            for (var i = 0; i < _contents.Count; i++)
            {
                var reagent = _contents[i];
                var ratio = (remainingVolume - quantity).Double() / remainingVolume.Double();
                remainingVolume -= reagent.Quantity;

                var newQuantity = reagent.Quantity * ratio;
                var splitQuantity = reagent.Quantity - newQuantity;

                _contents[i] = new ReagentQuantity(reagent.ReagentId, newQuantity);
                newSolution._contents.Add(new ReagentQuantity(reagent.ReagentId, splitQuantity));
                newTotalVolume += splitQuantity;
                quantity -= splitQuantity;
            }

            newSolution.TotalVolume = newTotalVolume;
            TotalVolume -= newTotalVolume;

            return newSolution;
        }

        public void AddSolution(Solution otherSolution)
        {
            for (var i = 0; i < otherSolution._contents.Count; i++)
            {
                var otherReagent = otherSolution._contents[i];

                var found = false;
                for (var j = 0; j < _contents.Count; j++)
                {
                    var reagent = _contents[j];
                    if (reagent.ReagentId == otherReagent.ReagentId)
                    {
                        found = true;
                        _contents[j] = new ReagentQuantity(reagent.ReagentId, reagent.Quantity + otherReagent.Quantity);
                        break;
                    }
                }

                if (!found)
                {
                    _contents.Add(new ReagentQuantity(otherReagent.ReagentId, otherReagent.Quantity));
                }
            }

            TotalVolume += otherSolution.TotalVolume;
        }

        private Color GetColor()
        {
            if (TotalVolume == 0)
            {
                return Color.Transparent;
            }

            Color mixColor = default;
            var runningTotalQuantity = ReagentUnit.New(0);
            var protoManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var reagent in Contents)
            {
                runningTotalQuantity += reagent.Quantity;

                if (!protoManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto))
                {
                    continue;
                }

                if (mixColor == default)
                {
                    mixColor = proto.SubstanceColor;
                    continue;
                }

                var interpolateValue = (1 / runningTotalQuantity.Float()) * reagent.Quantity.Float();
                mixColor = Color.InterpolateBetween(mixColor, proto.SubstanceColor, interpolateValue);
            }
            return mixColor;
        }

        public Solution Clone()
        {
            var volume = ReagentUnit.New(0);
            var newSolution = new Solution();

            for (var i = 0; i < _contents.Count; i++)
            {
                var reagent = _contents[i];
                newSolution._contents.Add(reagent);
                volume += reagent.Quantity;
            }

            newSolution.TotalVolume = volume;
            return newSolution;
        }

        public void DoEntityReaction(IEntity entity, ReactionMethod method)
        {
            var chemistry = EntitySystem.Get<ChemistrySystem>();

            foreach (var (reagentId, quantity) in _contents.ToArray())
            {
                chemistry.ReactionEntity(entity, method, reagentId, quantity, this);
            }
        }

        [Serializable, NetSerializable]
        [DataDefinition]
        public readonly struct ReagentQuantity: IComparable<ReagentQuantity>
        {
            [DataField("ReagentId", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentPrototype>))]
            public readonly string ReagentId;
            [DataField("Quantity")]
            public readonly ReagentUnit Quantity;

            public ReagentQuantity(string reagentId, ReagentUnit quantity)
            {
                ReagentId = reagentId;
                Quantity = quantity;
            }

            [ExcludeFromCodeCoverage]
            public override string ToString()
            {
                return $"{ReagentId}:{Quantity}";
            }

            public int CompareTo(ReagentQuantity other) { return Quantity.Float().CompareTo(other.Quantity.Float()); }

            public void Deconstruct(out string reagentId, out ReagentUnit quantity)
            {
                reagentId = ReagentId;
                quantity = Quantity;
            }
        }

        #region Enumeration

        public IEnumerator<ReagentQuantity> GetEnumerator()
        {
            return _contents.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
