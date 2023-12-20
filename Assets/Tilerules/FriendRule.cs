using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
// A simple rule for parking lots. 
// A checkmark means a tile that is a "friend". An X is any other tile
// Most of the script was created automatically by Unity.
// Notibly, this rule script overrides 1 and 2(checkmark and X figures) for use in the editor.
public class FriendRule : RuleTile<NullRule.Neighbor> {
	public List<TileBase> friends;
    public class Neighbor : RuleTile.TilingRule.Neighbor {
        public const int NotFriend = 2;
		public const int Friend = 1;
        
    }

    public override bool RuleMatch(int neighbor, TileBase tile) {
        switch (neighbor) {
            case Neighbor.Friend: return friends.Contains(tile);
            case Neighbor.NotFriend: return !friends.Contains(tile);
        }
        return base.RuleMatch(neighbor, tile);
    }
}