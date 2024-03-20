using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Rewritten from scratch and maintained to present by VisEntities
 * Originally created by Sonny-Boi, up to version 1.0.0
 */

namespace Oxide.Plugins
{
    [Info("Inventory Lock", "VisEntities", "2.0.0")]
    [Description("Locks player's inventory when joining the server or upon entering certain zones.")]
    public class InventoryLock : RustPlugin
    {
        #region 3rd Party Dependencies

        [PluginReference]
        private readonly Plugin ZoneManager;

        #endregion 3rd Party Dependencies

        #region Fields

        private static InventoryLock _plugin;
        private static Configuration _config;

        public enum ContainerType
        {
            Belt,
            Clothing,
            Main,
            All
        }

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Lock On Connect")]
            public List<ContainerType> LockOnConnect { get; set; }

            [JsonProperty("Locked Zones")]
            public List<LockedZone> LockedZones { get; set; }

            public class LockedZone
            {
                [JsonProperty("Zone Id")]
                public string ZoneId { get; set; }

                [JsonProperty("Container Types")]
                public List<ContainerType> ContainerTypes { get; set; }
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                LockOnConnect = new List<ContainerType>
                {
                    ContainerType.Clothing,
                },
                LockedZones = new List<Configuration.LockedZone>
                {
                    new Configuration.LockedZone
                    {
                        ZoneId = "42626527",
                        ContainerTypes = new List<ContainerType>
                        {
                            ContainerType.Clothing,
                            ContainerType.Belt
                        }
                    },
                    new Configuration.LockedZone
                    {
                        ZoneId = "89401263",
                        ContainerTypes = new List<ContainerType>
                        {
                            ContainerType.All,
                        }
                    },
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player != null)
                {
                    LockOrUnlockContainerSlots(player, ContainerType.All, lockSlots: false);
                }
            }

            _config = null;
            _plugin = null;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (PermissionUtil.VerifyHasPermission(player, PermissionUtil.BYPASS))
                return;

            if (_config.LockOnConnect != null && _config.LockOnConnect.Count > 0)
            {
                foreach (ContainerType containerType in _config.LockOnConnect)
                {
                    LockOrUnlockContainerSlots(player, containerType, true);
                }
            }
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            LockOrUnlockContainerSlots(player, ContainerType.All, lockSlots: false);
        }

        #endregion Oxide Hooks

        #region Zone Hooks

        private void OnEnterZone(string zoneId, BasePlayer player)
        {
            if (PermissionUtil.VerifyHasPermission(player, PermissionUtil.BYPASS))
                return;

            foreach (var lockedZone in _config.LockedZones)
            {
                if (lockedZone.ZoneId != zoneId)
                    continue;

                foreach (ContainerType containerType in lockedZone.ContainerTypes)
                    LockOrUnlockContainerSlots(player, containerType, lockSlots: true);

                break;
            }
        }

        private void OnExitZone(string zoneId, BasePlayer player)
        {
            foreach (var lockedZone in _config.LockedZones)
            {
                if (lockedZone.ZoneId != zoneId)
                    continue;

                LockOrUnlockContainerSlots(player, ContainerType.All, lockSlots: false);
                break;
            }
        }

        #endregion Zone Hooks

        #region Functions

        public static void LockOrUnlockContainerSlots(BasePlayer player, ContainerType containerType, bool lockSlots)
        {
            List<ItemContainer> containers = new List<ItemContainer>();

            switch (containerType)
            {
                case ContainerType.Clothing:
                    containers.Add(player.inventory.containerWear);
                    break;
                case ContainerType.Belt:
                    containers.Add(player.inventory.containerBelt);
                    break;
                case ContainerType.Main:
                    containers.Add(player.inventory.containerMain);
                    break;
                case ContainerType.All:
                    containers.Add(player.inventory.containerWear);
                    containers.Add(player.inventory.containerBelt);
                    containers.Add(player.inventory.containerMain);
                    break;
            }

            foreach (var container in containers)
            {
                bool isCurrentlyLocked = container.HasFlag(ItemContainer.Flag.IsLocked);
                if (lockSlots && !isCurrentlyLocked)
                {
                    container.SetFlag(ItemContainer.Flag.IsLocked, true);
                }
                else if (!lockSlots && isCurrentlyLocked)
                {
                    container.SetFlag(ItemContainer.Flag.IsLocked, false);
                }
            }

            player.inventory.SendSnapshot();
        }

        #endregion Functions

        #region Helper Functions

        private static bool VerifyPluginBeingLoaded(Plugin plugin)
        {
            return plugin != null && plugin.IsLoaded ? true : false;
        }

        #endregion Helper Functions

        #region Utility Classes

        private static class PermissionUtil
        {
            public const string ADMIN = "inventorylock.admin";
            public const string BYPASS = "inventorylock.bypass";

            public static void RegisterPermissions()
            {
                _plugin.permission.RegisterPermission(ADMIN, _plugin);
                _plugin.permission.RegisterPermission(BYPASS, _plugin);
            }

            public static bool VerifyHasPermission(BasePlayer player, string permissionName = ADMIN)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Utility Classes

        #region Commands

        /// <summary>
        /// inv.lock <PlayerId or PlayerName> <ContainerType> <True or False>
        /// </summary>

        [ConsoleCommand("inv.lock")]
        private void cmdLockInventory(ConsoleSystem.Arg conArgs)
        {
            BasePlayer cmdSender = conArgs.Player();
            if (cmdSender == null)
                return;

            if (!PermissionUtil.VerifyHasPermission(cmdSender, PermissionUtil.ADMIN))
            {
                SendReplyToPlayer(cmdSender, Lang.AdminPermissionRequired);
                return;
            }

            if (!conArgs.HasArgs(3))
            {
                SendReplyToPlayer(cmdSender, Lang.InvalidArguments);
                return;
            }

            BasePlayer targetPlayer = conArgs.GetPlayer(0);
            string containerTypeArg = conArgs.GetString(1);
            bool lockSlotsArg = conArgs.GetBool(2);

            if (targetPlayer == null)
            {
                SendReplyToPlayer(cmdSender, Lang.PlayerNotFound);
                return;
            }

            if (PermissionUtil.VerifyHasPermission(targetPlayer, PermissionUtil.BYPASS))
            {
                SendReplyToPlayer(cmdSender, Lang.PlayerHasBypass, targetPlayer.displayName);
                return;
            }

            if (!Enum.TryParse(containerTypeArg, true, out ContainerType containerType))
            {
                SendReplyToPlayer(cmdSender, Lang.InvalidContainerType);
                return;
            }

            LockOrUnlockContainerSlots(targetPlayer, containerType, lockSlotsArg);

            string successLangKey;
            if (lockSlotsArg)
                successLangKey = Lang.LockSuccess;
            else
                successLangKey = Lang.UnlockSuccess;

            SendReplyToPlayer(cmdSender, successLangKey, containerType.ToString(), targetPlayer.displayName);
        }

        #endregion Commands

        #region Localization

        private class Lang
        {
            public const string InvalidArguments = "InvalidArguments";
            public const string PlayerNotFound = "PlayerNotFound";
            public const string InvalidContainerType = "InvalidContainerType";
            public const string LockSuccess = "LockSuccess";
            public const string UnlockSuccess = "UnlockSuccess";
            public const string PlayerHasBypass = "PlayerHasBypass ";
            public const string AdminPermissionRequired = "AdminPermissionRequired ";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.InvalidArguments] = "Invalid arguments provided. Usage: inv.lock <PlayerId or PlayerName> <ContainerType> <True or False>",
                [Lang.PlayerNotFound] = "Player not found.",
                [Lang.InvalidContainerType] = "Invalid container type provided.",
                [Lang.LockSuccess] = "Successfully locked <color=#ffb347>{0}</color> container for <color=#ffb347>{1}</color>.",
                [Lang.UnlockSuccess] = "Successfully unlocked <color=#ffb347>{0}</color> container for <color=#ffb347>{1}</color>.",
                [Lang.PlayerHasBypass] = "<color=#ffb347>{0}</color> has bypass permission and cannot have their inventory locked or unlocked.",
                [Lang.AdminPermissionRequired] = "You do not have the required admin permission to execute this command.",
            }, this, "en");
        }

        private void SendReplyToPlayer(BasePlayer player, string messageKey, params object[] args)
        {
            string message = lang.GetMessage(messageKey, this, player.UserIDString);
            if (args.Length > 0)
                message = string.Format(message, args);

            SendReply(player, message);
        }

        #endregion Localization
    }
}