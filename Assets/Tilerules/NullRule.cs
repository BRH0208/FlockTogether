using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
// A simple rule for the islands. 
// A checkmark means any non-null(not water) tile, and an X means a null(water) tile
// Most of the script was created automatically by Unity.
// Notibly, this rule script overrides 1 and 2(checkmark and X figures) for use in the editor.
public class NullRule : RuleTile<NullRule.Neighbor> {
    public class Neighbor : RuleTile.TilingRule.Neighbor {
        public const int NotNull = 1;
		public const int Null = 2;
        
    }

    public override bool RuleMatch(int neighbor, TileBase tile) {
        switch (neighbor) {
            case Neighbor.Null: return tile == null;
            case Neighbor.NotNull: return tile != null;
        }
        return base.RuleMatch(neighbor, tile);
    }
}