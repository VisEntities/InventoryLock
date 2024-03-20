This plugin allows you to lock or unlock players' inventories automatically upon joining the server, entering certain zones, or manually through a command.

[Demonstration](https://youtu.be/23EIkbAECcU)

-----------------

## Container Types
- `Belt (0)` - The hotbar area where players keep their quick access items.
- `Clothing (1)` - The area where players equip their armor and clothing items.
- `Main (2)` - The main inventory space where most of the items are stored.
- `All (3)` - Includes all inventory types.

-------------

## Permissions

- `inventorylock.admin` - Allows to use the `inv.lock` command to lock or unlock inventories for other players.
- `inventorylock.bypass` - Allows to be unaffected by automatic inventory locking or the `inv.lock` command.

-----------------------------

## Console Commands

- `inv.lock <PlayerId or PlayerName> <ContainerType> <True or False>` - Locks or unlocks specified inventory container types for a given player. 

-----------------------------

## Configuration
```json
{
  "Version": "2.0.0",
  "Lock On Connect": [
    0
  ],
  "Locked Zones": [
    {
      "Zone Id": "42626527",
      "Container Types": [
        0,
        1
      ]
    },
    {
      "Zone Id": "89401263",
      "Container Types": [
        3
      ]
    }
  ]
}
```

-----------------------------------

## Localization

```json
{
  "InvalidArguments": "Invalid arguments provided. Usage: inv.lock <PlayerId or PlayerName> <ContainerType> <True or False>",
  "PlayerNotFound": "Player not found.",
  "InvalidContainerType": "Invalid container type provided.",
  "LockSuccess": "Successfully locked <color=#ffb347>{0}</color> container for <color=#ffb347>{1}</color>.",
  "UnlockSuccess": "Successfully unlocked <color=#ffb347>{0}</color> container for <color=#ffb347>{1}</color>.",
  "PlayerHasBypass ": "<color=#ffb347>{0}</color> has bypass permission and cannot have their inventory locked or unlocked.",
  "AdminPermissionRequired ": "You do not have the required admin permission to execute this command."
}
```

---------

## Credits
 * Rewritten from scratch and maintained to present by **VisEntities**
 * Originally created by **Sonny-Boi**, up to version 1.0.0