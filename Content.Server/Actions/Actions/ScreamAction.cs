using Content.Server.CharacterAppearance.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Actions.Components;
using Content.Shared.Audio;
using Content.Shared.CharacterAppearance;
using Content.Shared.Cooldown;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using System;

namespace Content.Server.Actions.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class ScreamAction : IInstantAction
    {
        private const float Variation = 0.125f;
        private const float Volume = 4f;

        [Dependency] private readonly IRobustRandom _random = default!;

        [DataField("male", required: true)] private SoundSpecifier _male = default!;
        [DataField("female", required: true)] private SoundSpecifier _female = default!;
        [DataField("wilhelm", required: true)] private SoundSpecifier _wilhelm = default!;

        /// seconds
        [DataField("cooldown")] private float _cooldown = 10;

        public ScreamAction()
        {
            IoCManager.InjectDependencies(this);
        }

        public void DoInstantAction(InstantActionEventArgs args)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanSpeak(args.Performer)) return;
            if (!args.Performer.TryGetComponent<HumanoidAppearanceComponent>(out var humanoid)) return;
            if (!args.Performer.TryGetComponent<SharedActionsComponent>(out var actions)) return;

            if (_random.Prob(.01f))
            {
                SoundSystem.Play(Filter.Pvs(args.Performer), _wilhelm.GetSound(), args.Performer, AudioParams.Default.WithVolume(Volume));
            }
            else
            {
                switch (humanoid.Sex)
                {
                    case Sex.Male:
                        SoundSystem.Play(Filter.Pvs(args.Performer), _male.GetSound(), args.Performer, AudioHelpers.WithVariation(Variation).WithVolume(Volume));
                        break;
                    case Sex.Female:
                        SoundSystem.Play(Filter.Pvs(args.Performer), _female.GetSound(), args.Performer, AudioHelpers.WithVariation(Variation).WithVolume(Volume));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            actions.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(_cooldown));
        }
    }
}
