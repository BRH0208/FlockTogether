using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
// Third option tilerule, a basic tilerule for giving a third option referecing another type of tile.
// Designed for use with roads
public class ThirdOptionTilerule : RuleTile<ThirdOptionTilerule.Neighbor> {
    
	public TileBase[] optionTiles;
    public class Neighbor : RuleTile.TilingRule.Neighbor {
        public const int Connection = 3;
    }

    public override bool RuleMatch(int neighbor, TileBase tile) {
        switch (neighbor) {
            case Neighbor.Connection: return optionTiles.Contains(tile);
        }
        return base.RuleMatch(neighbor, tile);
    }
	
	
}