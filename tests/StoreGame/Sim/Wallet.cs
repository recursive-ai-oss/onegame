using OneGame.Core;

namespace StoreGame;

/// <summary>What the player has bought, as game state: coins and the ad-free unlock. Purchases change it only through commands, so a replay of the command log reproduces them.</summary>
[Entity]
public partial class Wallet : Entity
{
    /// <summary>Coins from consumable purchases.</summary>
    [Synced] public partial int Coins { get; private set; }

    /// <summary>True once the one-time "remove ads" product is owned.</summary>
    [Synced] public partial bool AdsRemoved { get; private set; }

    /// <summary>How many purchases have been granted, restores included.</summary>
    [Synced] public partial int Grants { get; private set; }

    /// <summary>Adds coins from a consumable purchase.</summary>
    [Command]
    public void Credit(int coins)
    {
        Coins += coins;
        Grants++;
    }

    /// <summary>Sets the ad-free unlock; a restore sets it again, a refund clears it.</summary>
    [Command]
    public void SetAdsRemoved(bool removed)
    {
        AdsRemoved = removed;
        Grants++;
    }
}
