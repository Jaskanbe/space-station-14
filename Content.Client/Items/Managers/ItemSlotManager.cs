using System;
using System.Collections.Generic;
using Content.Client.Examine;
using Content.Client.Items.UI;
using Content.Client.Storage;
using Content.Client.Verbs;
using Content.Shared.Cooldown;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Items.Managers
{
    public class ItemSlotManager : IItemSlotManager
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IUserInterfaceManager _uiMgr = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IComponentManager _componentManager = default!;

        private readonly HashSet<EntityUid> _highlightEntities = new();

        public event Action<EntitySlotHighlightedEventArgs>? EntityHighlightedUpdated;

        public bool SetItemSlot(ItemSlotButton button, IEntity? entity)
        {
            if (entity == null)
            {
                button.SpriteView.Sprite = null;
                button.StorageButton.Visible = false;
            }
            else
            {
                ISpriteComponent? sprite;
                if (entity.TryGetComponent(out HandVirtualPullComponent? virtPull)
                    && _componentManager.TryGetComponent(virtPull.PulledEntity, out ISpriteComponent pulledSprite))
                {
                    sprite = pulledSprite;
                }
                else if (!entity.TryGetComponent(out sprite))
                {
                    return false;
                }

                button.ClearHover();
                button.SpriteView.Sprite = sprite;
                button.StorageButton.Visible = entity.HasComponent<ClientStorageComponent>();
            }

            button.Entity = entity?.Uid ?? default;

            // im lazy
            button.UpdateSlotHighlighted();
            return true;
        }

        public bool OnButtonPressed(GUIBoundKeyEventArgs args, IEntity? item)
        {
            if (item == null)
                return false;

            if (args.Function == ContentKeyFunctions.ExamineEntity)
            {
                _entitySystemManager.GetEntitySystem<ExamineSystem>()
                    .DoExamine(item);
            }
            else if (args.Function == ContentKeyFunctions.OpenContextMenu)
            {
                _entitySystemManager.GetEntitySystem<VerbSystem>()
                                    .OpenContextMenu(item, _uiMgr.ScreenToUIPosition(args.PointerLocation));
            }
            else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
            {
                var inputSys = _entitySystemManager.GetEntitySystem<InputSystem>();

                var func = args.Function;
                var funcId = _inputManager.NetworkBindMap.KeyFunctionID(args.Function);


                var mousePosWorld = _eyeManager.ScreenToMap(args.PointerLocation);

                var coordinates = _mapManager.TryFindGridAt(mousePosWorld, out var grid) ? grid.MapToGrid(mousePosWorld) :
                    EntityCoordinates.FromMap(_mapManager, mousePosWorld);

                var message = new FullInputCmdMessage(_gameTiming.CurTick, _gameTiming.TickFraction, funcId, BoundKeyState.Down,
                    coordinates, args.PointerLocation, item.Uid);

                // client side command handlers will always be sent the local player session.
                var session = _playerManager.LocalPlayer?.Session;
                if (session == null)
                    return false;

                inputSys.HandleInputCommand(session, func, message);
            }
            else
            {
                return false;
            }
            args.Handle();
            return true;
        }

        public void UpdateCooldown(ItemSlotButton? button, IEntity? entity)
        {
            var cooldownDisplay = button?.CooldownDisplay;

            if (cooldownDisplay == null)
            {
                return;
            }

            if (entity == null ||
                entity.Deleted ||
                !entity.TryGetComponent(out ItemCooldownComponent? cooldown) ||
                !cooldown.CooldownStart.HasValue ||
                !cooldown.CooldownEnd.HasValue)
            {
                cooldownDisplay.Visible = false;
                return;
            }

            var start = cooldown.CooldownStart.Value;
            var end = cooldown.CooldownEnd.Value;

            var length = (end - start).TotalSeconds;
            var progress = (_gameTiming.CurTime - start).TotalSeconds / length;
            var ratio = (progress <= 1 ? (1 - progress) : (_gameTiming.CurTime - end).TotalSeconds * -5);

            cooldownDisplay.Progress = MathHelper.Clamp((float) ratio, -1, 1);
            cooldownDisplay.Visible = ratio > -1f;
        }

        public void HoverInSlot(ItemSlotButton button, IEntity? entity, bool fits)
        {
            if (entity == null || !button.MouseIsHovering)
            {
                button.ClearHover();
                return;
            }

            if (!entity.HasComponent<SpriteComponent>())
            {
                return;
            }

            // Set green / red overlay at 50% transparency
            var hoverEntity = _entityManager.SpawnEntity("hoverentity", MapCoordinates.Nullspace);
            var hoverSprite = hoverEntity.GetComponent<SpriteComponent>();
            hoverSprite.CopyFrom(entity.GetComponent<SpriteComponent>());
            hoverSprite.Color = fits ? new Color(0, 255, 0, 127) : new Color(255, 0, 0, 127);

            button.HoverSpriteView.Sprite = hoverSprite;
        }

        public bool IsHighlighted(EntityUid uid)
        {
            return _highlightEntities.Contains(uid);
        }

        public void HighlightEntity(EntityUid uid)
        {
            if (!_highlightEntities.Add(uid))
                return;

            EntityHighlightedUpdated?.Invoke(new EntitySlotHighlightedEventArgs(uid, true));
        }

        public void UnHighlightEntity(EntityUid uid)
        {
            if (!_highlightEntities.Remove(uid))
                return;

            EntityHighlightedUpdated?.Invoke(new EntitySlotHighlightedEventArgs(uid, false));
        }
    }

    public readonly struct EntitySlotHighlightedEventArgs
    {
        public EntitySlotHighlightedEventArgs(EntityUid entity, bool newHighlighted)
        {
            Entity = entity;
            NewHighlighted = newHighlighted;
        }

        public EntityUid Entity { get; }
        public bool NewHighlighted { get; }
    }
}
